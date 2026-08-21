using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Application.Options;
using CentralIdentity.Application.Services;
using CentralIdentity.Contracts.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CentralIdentity.Api.Controllers;

/// <summary>
/// OAuth2 authorization_code and refresh_token endpoints plus revocation and userinfo.
/// </summary>
[ApiController]
[Route("connect")]
public sealed class ConnectController : ControllerBase
{
    private readonly IApplicationRepository _appRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUserApplicationRepository _userAppRepo;
    private readonly IAuthorizationCodeService _authCodeService;
    private readonly ITokenService _tokenService;
    private readonly IClientSecretHasher _clientSecretHasher;
    private readonly OAuthOptions _oauthOptions;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<ConnectController> _logger;

    public ConnectController(
        IApplicationRepository appRepo,
        IUserRepository userRepo,
        IUserApplicationRepository userAppRepo,
        IAuthorizationCodeService authCodeService,
        ITokenService tokenService,
        IClientSecretHasher clientSecretHasher,
        IOptions<OAuthOptions> oauthOptions,
        IOptions<JwtOptions> jwtOptions,
        ILogger<ConnectController> logger)
    {
        _appRepo = appRepo;
        _userRepo = userRepo;
        _userAppRepo = userAppRepo;
        _authCodeService = authCodeService;
        _tokenService = tokenService;
        _clientSecretHasher = clientSecretHasher;
        _oauthOptions = oauthOptions.Value;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    [HttpGet("authorize")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Authorize(
        [FromQuery(Name = "response_type")] string? responseType,
        [FromQuery(Name = "client_id")] string? clientId,
        [FromQuery(Name = "redirect_uri")] string? redirectUri,
        [FromQuery(Name = "scope")] string? scope,
        [FromQuery(Name = "state")] string? state,
        [FromQuery(Name = "code_challenge")] string? codeChallenge,
        [FromQuery(Name = "code_challenge_method")] string? codeChallengeMethod,
        [FromQuery(Name = "user_id")] long? userId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(responseType) || !_oauthOptions.AllowedResponseTypes.Contains(responseType))
            return BadRequest(new OAuthErrorResponse { Error = "unsupported_response_type", ErrorDescription = "response_type must be 'code'." });

        if (string.IsNullOrWhiteSpace(clientId))
            return BadRequest(new OAuthErrorResponse { Error = "invalid_request", ErrorDescription = "client_id is required." });

        if (string.IsNullOrWhiteSpace(redirectUri))
            return BadRequest(new OAuthErrorResponse { Error = "invalid_request", ErrorDescription = "redirect_uri is required." });

        if (userId is null)
            return BadRequest(new OAuthErrorResponse { Error = "login_required", ErrorDescription = "user_id is required (no active session)." });

        var app = await _appRepo.GetByClientIdAsync(clientId, ct);
        if (app is null || !app.IsActive)
            return BadRequest(new OAuthErrorResponse { Error = "unauthorized_client", ErrorDescription = "Unknown or inactive client." });

        if (!app.GetRedirectUris().Contains(redirectUri, StringComparer.Ordinal))
            return BadRequest(new OAuthErrorResponse { Error = "invalid_request", ErrorDescription = "redirect_uri is not registered for this client." });

        var user = await _userRepo.GetByIdAsync(userId.Value, ct);
        if (user is null || !user.IsActive)
            return BadRequest(new OAuthErrorResponse { Error = "access_denied", ErrorDescription = "User is unknown or inactive." });

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
            return BadRequest(new OAuthErrorResponse { Error = "access_denied", ErrorDescription = "Account is locked." });

        var assignment = await _userAppRepo.GetAsync(user.UserId, app.ApplicationId, ct);
        if (assignment is null || !assignment.IsActive)
            return BadRequest(new OAuthErrorResponse { Error = "access_denied", ErrorDescription = "User does not have access to this application." });

        var codeResult = await _authCodeService.CreateAuthorizationCodeAsync(new CreateAuthorizationCodeCommand(
            user.UserId, app.ApplicationId, app.ClientId, redirectUri, scope ?? string.Empty, codeChallenge, codeChallengeMethod), ct);

        if (codeResult.IsFailure)
            return BadRequest(new OAuthErrorResponse { Error = "invalid_request", ErrorDescription = codeResult.Error });

        var separator = redirectUri.Contains('?') ? '&' : '?';
        var location = $"{redirectUri}{separator}code={Uri.EscapeDataString(codeResult.Value)}";
        if (!string.IsNullOrEmpty(state))
            location += $"&state={Uri.EscapeDataString(state)}";

        return Redirect(location);
    }

    [HttpPost("token")]
    [EnableRateLimiting("token")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OAuthErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Token(
        [FromForm(Name = "grant_type")] string? grantType,
        [FromForm(Name = "code")] string? code,
        [FromForm(Name = "redirect_uri")] string? redirectUri,
        [FromForm(Name = "client_id")] string? clientId,
        [FromForm(Name = "client_secret")] string? clientSecret,
        [FromForm(Name = "code_verifier")] string? codeVerifier,
        [FromForm(Name = "refresh_token")] string? refreshToken,
        CancellationToken ct)
    {
        var request = new TokenRequest
        {
            GrantType = grantType ?? string.Empty,
            Code = code,
            RedirectUri = redirectUri,
            ClientId = clientId,
            ClientSecret = clientSecret,
            CodeVerifier = codeVerifier,
            RefreshToken = refreshToken
        };

        return request.GrantType switch
        {
            "authorization_code" => await ExchangeAuthorizationCodeAsync(request, ct),
            "refresh_token" => await RefreshTokensAsync(request, ct),
            _ => BadRequest(new OAuthErrorResponse { Error = "unsupported_grant_type", ErrorDescription = "Only authorization_code and refresh_token are supported." })
        };
    }

    [HttpPost("revoke")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Revoke(
        [FromForm(Name = "token")] string? token,
        [FromForm(Name = "token_type_hint")] string? tokenTypeHint,
        [FromForm(Name = "client_id")] string? clientId,
        [FromForm(Name = "client_secret")] string? clientSecret,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(clientId))
        {
            return BadRequest(new OAuthErrorResponse
            {
                Error = "invalid_request",
                ErrorDescription = "token and client_id are required."
            });
        }

        try
        {
            var app = await ValidateClientAsync(clientId, clientSecret, ct);
            if (app is null)
                return Ok();

            if (tokenTypeHint is null || string.Equals(tokenTypeHint, "refresh_token", StringComparison.Ordinal))
                await _tokenService.RevokeRefreshTokenAsync(token, clientId, ct);

            return Ok();
        }
        catch (TokenRequestException ex)
        {
            return CreateErrorResult(ex);
        }
    }

    [HttpGet("userinfo")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UserInfo(CancellationToken ct)
    {
        var subClaim = User.FindFirst(JwtRegisteredClaimNames.Sub) ?? User.FindFirst(ClaimTypes.NameIdentifier);
        var clientIdClaim = User.FindFirst("client_id");
        var audienceClaim = User.FindFirst("aud");

        if (subClaim is null || clientIdClaim is null || !long.TryParse(subClaim.Value, out var userId))
            return Unauthorized(new OAuthErrorResponse { Error = "invalid_token", ErrorDescription = "Token is missing required claims." });

        var app = await _appRepo.GetByClientIdAsync(clientIdClaim.Value, ct);
        if (app is null || !app.IsActive)
            return Unauthorized(new OAuthErrorResponse { Error = "invalid_token", ErrorDescription = "Token references an unknown or inactive client." });

        if (audienceClaim is null || !string.Equals(audienceClaim.Value, app.Audience, StringComparison.Ordinal))
            return Unauthorized(new OAuthErrorResponse { Error = "invalid_token", ErrorDescription = "Token audience does not match the client." });

        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null || !user.IsActive)
            return Unauthorized(new OAuthErrorResponse { Error = "invalid_token", ErrorDescription = "User is unknown or inactive." });

        return Ok(new UserInfoResponse
        {
            Subject = user.UserId.ToString(),
            PreferredUsername = user.Username,
            Email = user.Email,
            EmailVerified = user.EmailVerified,
            GivenName = user.FirstName,
            FamilyName = user.LastName,
            PhoneNumber = user.Phone,
            PhoneNumberVerified = user.PhoneVerified
        });
    }

    private async Task<IActionResult> ExchangeAuthorizationCodeAsync(TokenRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.RedirectUri))
            return BadRequest(new OAuthErrorResponse { Error = "invalid_request", ErrorDescription = "code, client_id and redirect_uri are required." });

        CentralIdentity.Domain.Entities.IdentityApplication? app;
        try
        {
            app = await ValidateClientAsync(request.ClientId, request.ClientSecret, ct);
        }
        catch (TokenRequestException ex)
        {
            return CreateErrorResult(ex);
        }

        if (app is null)
            return Unauthorized(new OAuthErrorResponse { Error = "invalid_client", ErrorDescription = "Unknown or inactive client." });

        var codeValidation = await _authCodeService.ValidateAndConsumeAsync(new ValidateAuthorizationCodeCommand(
            request.Code, request.ClientId, request.RedirectUri, request.CodeVerifier), ct);

        if (codeValidation.IsFailure)
            return BadRequest(new OAuthErrorResponse { Error = "invalid_grant", ErrorDescription = codeValidation.Error });

        var authCode = codeValidation.Value;
        var user = await _userRepo.GetByIdAsync(authCode.UserId, ct);
        if (user is null || !user.IsActive)
            return BadRequest(new OAuthErrorResponse { Error = "invalid_grant", ErrorDescription = "User is unknown or inactive." });

        var assignment = await _userAppRepo.GetAsync(user.UserId, app.ApplicationId, ct);
        if (assignment is null || !assignment.IsActive)
            return BadRequest(new OAuthErrorResponse { Error = "invalid_grant", ErrorDescription = "User access to this application has been revoked." });

        var scopes = authCode.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        try
        {
            var result = await _tokenService.IssueTokensAsync(user, app, scopes, GetIpAddress(), GetUserAgent(), ct);
            return Ok(new TokenResponse
            {
                AccessToken = result.accessToken,
                RefreshToken = result.refreshToken,
                SessionId = result.session.SessionId.ToString(),
                TokenType = "Bearer",
                ExpiresIn = _jwtOptions.AccessTokenLifetimeMinutes * 60,
                Scope = authCode.Scope
            });
        }
        catch (TokenRequestException ex)
        {
            return CreateErrorResult(ex);
        }
    }

    private async Task<IActionResult> RefreshTokensAsync(TokenRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new OAuthErrorResponse { Error = "invalid_request", ErrorDescription = "client_id and refresh_token are required." });

        try
        {
            var app = await ValidateClientAsync(request.ClientId, request.ClientSecret, ct);
            if (app is null)
                return Unauthorized(new OAuthErrorResponse { Error = "invalid_client", ErrorDescription = "Unknown or inactive client." });

            var result = await _tokenService.RefreshAsync(request.RefreshToken, request.ClientId, GetIpAddress(), GetUserAgent(), ct);
            return Ok(new TokenResponse
            {
                AccessToken = result.accessToken,
                RefreshToken = result.refreshToken,
                SessionId = ReadSessionId(result.accessToken),
                TokenType = "Bearer",
                ExpiresIn = _jwtOptions.AccessTokenLifetimeMinutes * 60,
                Scope = ReadScope(result.accessToken)
            });
        }
        catch (TokenRequestException ex)
        {
            return CreateErrorResult(ex);
        }
    }

    private async Task<CentralIdentity.Domain.Entities.IdentityApplication?> ValidateClientAsync(string clientId, string? clientSecret, CancellationToken ct)
    {
        var app = await _appRepo.GetByClientIdAsync(clientId, ct);
        if (app is null || !app.IsActive)
            return null;

        if (string.Equals(app.ClientType, "Confidential", StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new TokenRequestException("invalid_client", "client_secret is required for confidential clients.");

            if (app.ClientSecretHash is null)
            {
                _logger.LogError("Confidential application {ApplicationId} ({ClientId}) has no ClientSecretHash configured.", app.ApplicationId, app.ClientId);
                throw new TokenRequestException("server_error", "Client is misconfigured.");
            }

            if (!_clientSecretHasher.VerifySecret(clientSecret, app.ClientSecretHash))
                throw new TokenRequestException("invalid_client", "client_secret is invalid.");
        }

        return app;
    }

    private IActionResult CreateErrorResult(TokenRequestException ex)
    {
        var payload = new OAuthErrorResponse { Error = ex.Error, ErrorDescription = ex.Description };
        return ex.Error switch
        {
            "invalid_client" => Unauthorized(payload),
            "server_error" => StatusCode(StatusCodes.Status500InternalServerError, payload),
            _ => BadRequest(payload)
        };
    }

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
    private string? GetUserAgent() => Request.Headers.UserAgent.Count == 0 ? null : Request.Headers.UserAgent.ToString();

    private static string? ReadSessionId(string accessToken)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        return jwt.Claims.FirstOrDefault(c => c.Type == "session_id")?.Value;
    }

    private static string ReadScope(string accessToken)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        return string.Join(' ', jwt.Claims.Where(c => c.Type == "scope").Select(c => c.Value));
    }
}
