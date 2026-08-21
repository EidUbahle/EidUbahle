using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Application.Options;
using CentralIdentity.Application.Services;
using CentralIdentity.Contracts.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CentralIdentity.Api.Controllers;

/// <summary>
/// OAuth2 authorization_code grant + PKCE endpoints (RFC 6749 / RFC 7636) and the
/// OIDC userinfo endpoint.
/// </summary>
[ApiController]
[Route("connect")]
public sealed class ConnectController : ControllerBase
{
    private readonly IApplicationRepository _appRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUserApplicationRepository _userAppRepo;
    private readonly IAuthorizationCodeService _authCodeService;
    private readonly IAccessTokenService _accessTokenService;
    private readonly OAuthOptions _oauthOptions;
    private readonly JwtOptions _jwtOptions;

    public ConnectController(
        IApplicationRepository appRepo,
        IUserRepository userRepo,
        IUserApplicationRepository userAppRepo,
        IAuthorizationCodeService authCodeService,
        IAccessTokenService accessTokenService,
        IOptions<OAuthOptions> oauthOptions,
        IOptions<JwtOptions> jwtOptions)
    {
        _appRepo = appRepo;
        _userRepo = userRepo;
        _userAppRepo = userAppRepo;
        _authCodeService = authCodeService;
        _accessTokenService = accessTokenService;
        _oauthOptions = oauthOptions.Value;
        _jwtOptions = jwtOptions.Value;
    }

    /// <summary>
    /// Authorization endpoint. Issues a short-lived, single-use authorization code and redirects
    /// the user agent back to the client's redirect_uri.
    /// NOTE: this platform does not yet implement an interactive login/session UI, so the
    /// already-authenticated user is identified via the <paramref name="userId"/> query parameter.
    /// </summary>
    [HttpGet("authorize")]
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

    /// <summary>Token endpoint. Exchanges an authorization code (+ PKCE verifier) for an access token.</summary>
    [HttpPost("token")]
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
        CancellationToken ct)
    {
        var request = new TokenRequest
        {
            GrantType = grantType ?? string.Empty,
            Code = code,
            RedirectUri = redirectUri,
            ClientId = clientId,
            ClientSecret = clientSecret,
            CodeVerifier = codeVerifier
        };

        if (!string.Equals(request.GrantType, "authorization_code", StringComparison.Ordinal))
            return BadRequest(new OAuthErrorResponse { Error = "unsupported_grant_type", ErrorDescription = "Only authorization_code is supported." });

        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.RedirectUri))
            return BadRequest(new OAuthErrorResponse { Error = "invalid_request", ErrorDescription = "code, client_id and redirect_uri are required." });

        var app = await _appRepo.GetByClientIdAsync(request.ClientId, ct);
        if (app is null || !app.IsActive)
            return BadRequest(new OAuthErrorResponse { Error = "invalid_client", ErrorDescription = "Unknown or inactive client." });

        if (string.Equals(app.ClientType, "Confidential", StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(request.ClientSecret) || app.ClientSecretHash is null)
                return Unauthorized(new OAuthErrorResponse { Error = "invalid_client", ErrorDescription = "client_secret is required for confidential clients." });
        }

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
        var accessToken = _accessTokenService.CreateAccessToken(user, app, scopes);

        return Ok(new TokenResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresIn = _jwtOptions.AccessTokenLifetimeMinutes * 60,
            Scope = authCode.Scope
        });
    }

    /// <summary>Returns claims about the currently authenticated resource owner.</summary>
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

        // Defense-in-depth: confirm the token's audience matches the audience registered for
        // the application identified by its client_id claim (guards against audience confusion
        // attacks where a token minted for one relying party is replayed against another).
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
}
