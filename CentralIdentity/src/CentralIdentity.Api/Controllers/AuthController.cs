using System.IdentityModel.Tokens.Jwt;
using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Contracts.Auth;
using CentralIdentity.Contracts.Common;
using CentralIdentity.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentralIdentity.Api.Controllers;

[Authorize]
[Route("api")]
public sealed class AuthController : ControllerBase
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public AuthController(
        ISessionRepository sessionRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuditLogRepository auditLogRepository)
    {
        _sessionRepository = sessionRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _auditLogRepository = auditLogRepository;
    }

    [HttpPost("auth/logout")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var sessionId = GetSessionId();
        var userId = GetUserId();
        if (sessionId is null || userId is null)
            return Unauthorized();

        await _sessionRepository.RevokeAsync(sessionId.Value, "User requested logout", ct);
        await _refreshTokenRepository.RevokeBySessionAsync(sessionId.Value, "User requested logout", ct);
        await LogAsync(userId.Value, null, "Logout", "Information", "User logged out of the current session.", ct);
        return Ok(ApiResponse.Ok());
    }

    [HttpPost("auth/logout-all")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        await _sessionRepository.RevokeAllByUserAsync(userId.Value, "User requested logout from all sessions", ct);
        await _refreshTokenRepository.RevokeAllByUserAsync(userId.Value, "User requested logout from all sessions", ct);
        await LogAsync(userId.Value, null, "LogoutAll", "Information", "User logged out of all sessions.", ct);
        return Ok(ApiResponse.Ok());
    }

    [HttpGet("auth/session")]
    [ProducesResponseType(typeof(ApiResponse<SessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentSession(CancellationToken ct)
    {
        var sessionId = GetSessionId();
        if (sessionId is null)
            return Unauthorized();

        var session = await _sessionRepository.GetByIdAsync(sessionId.Value, ct);
        if (session is null)
            return NotFound(ApiResponse<SessionResponse>.Fail("Session not found."));

        return Ok(ApiResponse<SessionResponse>.Ok(ToResponse(session)));
    }

    [HttpGet("users/{userId:long}/sessions")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SessionResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserSessions(long userId, CancellationToken ct)
    {
        if (!CanAccessUser(userId))
            return Forbid();

        var sessions = await _sessionRepository.GetActiveByUserAsync(userId, ct);
        return Ok(ApiResponse<IReadOnlyList<SessionResponse>>.Ok(sessions.Select(ToResponse).ToList()));
    }

    [HttpPost("sessions/{sessionId:guid}/revoke")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken ct)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, ct);
        if (session is null)
            return NotFound(ApiResponse.Fail("Session not found."));

        if (!CanAccessUser(session.UserId))
            return Forbid();

        await _sessionRepository.RevokeAsync(sessionId, "Session revoked by API request", ct);
        await _refreshTokenRepository.RevokeBySessionAsync(sessionId, "Session revoked by API request", ct);
        await LogAsync(GetUserId(), session.ApplicationId, "SessionRevoked", "Information", $"Session {sessionId} was revoked.", ct);
        return Ok(ApiResponse.Ok());
    }

    [HttpPost("users/{userId:long}/applications/{applicationId:long}/revoke-sessions")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RevokeUserApplicationSessions(long userId, long applicationId, CancellationToken ct)
    {
        if (!CanAccessUser(userId))
            return Forbid();

        await _sessionRepository.RevokeByUserApplicationAsync(userId, applicationId, "User/application sessions revoked by API request", ct);
        await _refreshTokenRepository.RevokeByUserApplicationAsync(userId, applicationId, "User/application sessions revoked by API request", ct);
        await LogAsync(GetUserId(), applicationId, "UserApplicationSessionsRevoked", "Information", $"All sessions for user {userId} and application {applicationId} were revoked.", ct);
        return Ok(ApiResponse.Ok());
    }

    private long? GetUserId()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.FindFirst("sub")?.Value;
        return long.TryParse(sub, out var userId) ? userId : null;
    }

    private Guid? GetSessionId()
    {
        var sessionId = User.FindFirst("session_id")?.Value;
        return Guid.TryParse(sessionId, out var parsed) ? parsed : null;
    }

    private bool CanAccessUser(long userId)
    {
        var currentUserId = GetUserId();
        if (currentUserId == userId)
            return true;

        return User.Claims.Any(c =>
            (string.Equals(c.Type, "role", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(c.Type, System.Security.Claims.ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)) &&
            (string.Equals(c.Value, "admin", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(c.Value, "administrator", StringComparison.OrdinalIgnoreCase)));
    }

    private async Task LogAsync(long? userId, long? applicationId, string eventType, string severity, string description, CancellationToken ct)
    {
        await _auditLogRepository.LogAsync(new IdentityAuditLog
        {
            UserId = userId,
            ApplicationId = applicationId,
            EventType = eventType,
            Severity = severity,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            Description = description,
            CorrelationId = HttpContext.TraceIdentifier,
            CreatedAtUtc = DateTime.UtcNow
        }, ct);
    }

    private static SessionResponse ToResponse(IdentitySession session) => new()
    {
        SessionId = session.SessionId,
        UserId = session.UserId,
        ApplicationId = session.ApplicationId,
        ClientId = session.ClientId,
        CreatedAtUtc = session.CreatedAtUtc,
        LastActivityAtUtc = session.LastActivityAtUtc,
        ExpiresAtUtc = session.ExpiresAtUtc,
        IpAddress = session.IpAddress,
        UserAgent = session.UserAgent,
        DeviceId = session.DeviceId,
        IsActive = session.IsActive && session.RevokedAtUtc is null && session.ExpiresAtUtc > DateTime.UtcNow
    };
}
