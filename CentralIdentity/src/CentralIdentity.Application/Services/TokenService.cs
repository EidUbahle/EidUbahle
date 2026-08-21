using System.Globalization;
using System.Security.Cryptography;
using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Application.Options;
using CentralIdentity.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CentralIdentity.Application.Services;

public sealed class TokenService : ITokenService
{
    private readonly IAccessTokenService _accessTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUserRepository _userRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IDateTime _dateTime;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        IAccessTokenService accessTokenService,
        IRefreshTokenRepository refreshTokenRepository,
        ISessionRepository sessionRepository,
        IAuditLogRepository auditLogRepository,
        IUserRepository userRepository,
        IApplicationRepository applicationRepository,
        IDateTime dateTime,
        IOptions<JwtOptions> jwtOptions,
        ILogger<TokenService> logger)
    {
        _accessTokenService = accessTokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _sessionRepository = sessionRepository;
        _auditLogRepository = auditLogRepository;
        _userRepository = userRepository;
        _applicationRepository = applicationRepository;
        _dateTime = dateTime;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public async Task<(string accessToken, string refreshToken, IdentitySession session)> IssueTokensAsync(
        IdentityUser user,
        IdentityApplication application,
        IEnumerable<string> scopes,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct)
    {
        var now = _dateTime.UtcNow;
        var scopeList = scopes.Where(static s => !string.IsNullOrWhiteSpace(s)).ToArray();
        var session = new IdentitySession
        {
            SessionId = Guid.NewGuid(),
            UserId = user.UserId,
            ApplicationId = application.ApplicationId,
            ClientId = application.ClientId,
            CreatedAtUtc = now,
            LastActivityAtUtc = now,
            ExpiresAtUtc = now.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SecurityStamp = user.SecurityStamp,
            IsActive = true
        };

        await _sessionRepository.CreateAsync(session, ct);

        var refreshToken = GenerateRefreshToken();
        var refreshTokenEntity = new IdentityRefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = user.UserId,
            ApplicationId = application.ApplicationId,
            SessionId = session.SessionId,
            TokenHash = HashRefreshToken(refreshToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = session.ExpiresAtUtc,
            TokenFamilyId = Guid.NewGuid(),
            CreatedIpAddress = ipAddress,
            LastUsedIpAddress = ipAddress,
            UserAgent = userAgent,
            Scope = string.Join(' ', scopeList)
        };

        await _refreshTokenRepository.CreateAsync(refreshTokenEntity, ct);

        var accessToken = _accessTokenService.CreateAccessToken(user, application, scopeList, session.SessionId);

        await LogAsync(new IdentityAuditLog
        {
            UserId = user.UserId,
            ApplicationId = application.ApplicationId,
            EventType = "LoginSuccess",
            Severity = "Information",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Description = "Issued a new access token, refresh token, and session.",
            CreatedAtUtc = now
        }, ct);

        return (accessToken, refreshToken, session);
    }

    public async Task<(string accessToken, string refreshToken)> RefreshAsync(
        string refreshToken,
        string clientId,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct)
    {
        var now = _dateTime.UtcNow;
        var storedToken = await GetStoredTokenAsync(refreshToken, ct);
        var application = await _applicationRepository.GetByClientIdAsync(clientId, ct);

        if (application is null || !application.IsActive)
            throw new TokenRequestException("invalid_grant", "Refresh token does not belong to the specified client.");

        var session = await _sessionRepository.GetByIdAsync(storedToken.SessionId, ct);
        if (session is null)
            throw new TokenRequestException("invalid_grant", "Refresh token session is invalid.");

        if (storedToken.RevokedAtUtc.HasValue)
        {
            await HandleReuseDetectionAsync(storedToken, application, session, ipAddress, userAgent, ct);
            throw new TokenRequestException("invalid_grant", "Refresh token has already been revoked.");
        }

        if (storedToken.ExpiresAtUtc <= now)
            throw new TokenRequestException("invalid_grant", "Refresh token has expired.");

        if (storedToken.ApplicationId != application.ApplicationId || session.ApplicationId != application.ApplicationId || session.UserId != storedToken.UserId)
            throw new TokenRequestException("invalid_grant", "Refresh token does not belong to the specified client.");

        if (!session.IsActive || session.RevokedAtUtc.HasValue || session.ExpiresAtUtc <= now)
            throw new TokenRequestException("invalid_grant", "Refresh token session is no longer active.");

        var user = await _userRepository.GetByIdAsync(storedToken.UserId, ct);
        if (user is null || !user.IsActive)
            throw new TokenRequestException("invalid_grant", "Refresh token user is unknown or inactive.");

        if (!string.Equals(session.SecurityStamp, user.SecurityStamp, StringComparison.Ordinal))
        {
            await LogAsync(new IdentityAuditLog
            {
                UserId = user.UserId,
                ApplicationId = application.ApplicationId,
                EventType = "SecurityStampMismatch",
                Severity = "High",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Description = "Refresh token rejected because the session security stamp no longer matches the user security stamp.",
                CreatedAtUtc = now
            }, ct);

            throw new TokenRequestException("invalid_grant", "Refresh token session is no longer valid.");
        }

        await _refreshTokenRepository.RevokeAsync(storedToken.RefreshTokenId, "Rotated", ct);

        var nextRefreshToken = GenerateRefreshToken();
        await _refreshTokenRepository.CreateAsync(new IdentityRefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = storedToken.UserId,
            ApplicationId = storedToken.ApplicationId,
            SessionId = storedToken.SessionId,
            TokenHash = HashRefreshToken(nextRefreshToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
            TokenFamilyId = storedToken.TokenFamilyId,
            CreatedIpAddress = ipAddress,
            LastUsedIpAddress = ipAddress,
            UserAgent = userAgent,
            Scope = storedToken.Scope
        }, ct);

        await _sessionRepository.UpdateActivityAsync(session.SessionId, now, ct);

        var scopes = storedToken.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var accessToken = _accessTokenService.CreateAccessToken(user, application, scopes, session.SessionId);

        await LogAsync(new IdentityAuditLog
        {
            UserId = user.UserId,
            ApplicationId = application.ApplicationId,
            EventType = "RefreshSuccess",
            Severity = "Information",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Description = "Rotated the refresh token and issued a new access token.",
            CreatedAtUtc = now
        }, ct);

        return (accessToken, nextRefreshToken);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, string clientId, CancellationToken ct)
    {
        IdentityRefreshToken? storedToken;
        try
        {
            storedToken = await GetStoredTokenAsync(refreshToken, ct);
        }
        catch (TokenRequestException)
        {
            return;
        }

        var application = await _applicationRepository.GetByClientIdAsync(clientId, ct);
        if (application is null || application.ApplicationId != storedToken.ApplicationId)
            return;

        await _refreshTokenRepository.RevokeAsync(storedToken.RefreshTokenId, "Revoked by client request", ct);
    }

    public static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    public static string HashRefreshToken(string refreshToken)
    {
        try
        {
            var bytes = Base64UrlDecode(refreshToken);
            return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }
        catch (FormatException ex)
        {
            throw new TokenRequestException("invalid_grant", $"Refresh token format is invalid: {ex.Message}");
        }
    }

    private async Task<IdentityRefreshToken> GetStoredTokenAsync(string refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new TokenRequestException("invalid_grant", "refresh_token is required.");

        var tokenHash = HashRefreshToken(refreshToken);
        var storedToken = await _refreshTokenRepository.GetByHashAsync(tokenHash, ct);
        if (storedToken is null)
            throw new TokenRequestException("invalid_grant", "Refresh token is invalid.");

        return storedToken;
    }

    private async Task HandleReuseDetectionAsync(
        IdentityRefreshToken storedToken,
        IdentityApplication application,
        IdentitySession session,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct)
    {
        await _refreshTokenRepository.RevokeByFamilyAsync(storedToken.TokenFamilyId, "Refresh token reuse detected", ct);
        await _sessionRepository.RevokeAsync(storedToken.SessionId, "Refresh token reuse detected", ct);

        await LogAsync(new IdentityAuditLog
        {
            UserId = storedToken.UserId,
            ApplicationId = application.ApplicationId,
            EventType = "RefreshTokenReuseDetected",
            Severity = "High",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Description = FormattableString.Invariant($"Refresh token reuse was detected for session {session.SessionId} and the entire token family was revoked."),
            CreatedAtUtc = _dateTime.UtcNow
        }, ct);

        _logger.LogWarning(
            "Refresh token reuse detected for UserId {UserId}, ApplicationId {ApplicationId}, SessionId {SessionId}, FamilyId {FamilyId}",
            storedToken.UserId,
            storedToken.ApplicationId,
            storedToken.SessionId,
            storedToken.TokenFamilyId);
    }

    private Task LogAsync(IdentityAuditLog log, CancellationToken ct) => _auditLogRepository.LogAsync(log, ct);

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
    }
}
