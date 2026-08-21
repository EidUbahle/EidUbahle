using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;

namespace CentralIdentity.UnitTests.Fakes;

public sealed class FakeSessionRepository : ISessionRepository
{
    private readonly Dictionary<Guid, IdentitySession> _sessions = new();

    public IReadOnlyCollection<IdentitySession> Sessions => _sessions.Values.ToList().AsReadOnly();

    public Task<IdentitySession?> GetByIdAsync(Guid sessionId, CancellationToken ct) =>
        Task.FromResult(_sessions.TryGetValue(sessionId, out var session) ? session : null);

    public Task<IReadOnlyList<IdentitySession>> GetActiveByUserAsync(long userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var results = _sessions.Values.Where(s => s.UserId == userId && s.IsActive && s.RevokedAtUtc is null && s.ExpiresAtUtc > now).ToList();
        return Task.FromResult<IReadOnlyList<IdentitySession>>(results);
    }

    public Task<IReadOnlyList<IdentitySession>> GetActiveByUserApplicationAsync(long userId, long applicationId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var results = _sessions.Values.Where(s => s.UserId == userId && s.ApplicationId == applicationId && s.IsActive && s.RevokedAtUtc is null && s.ExpiresAtUtc > now).ToList();
        return Task.FromResult<IReadOnlyList<IdentitySession>>(results);
    }

    public Task CreateAsync(IdentitySession session, CancellationToken ct)
    {
        _sessions[session.SessionId] = session;
        return Task.CompletedTask;
    }

    public Task UpdateActivityAsync(Guid sessionId, DateTime lastActivityAtUtc, CancellationToken ct)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
            session.LastActivityAtUtc = lastActivityAtUtc;
        return Task.CompletedTask;
    }

    public Task RevokeAsync(Guid sessionId, string reason, CancellationToken ct)
    {
        if (_sessions.TryGetValue(sessionId, out var session) && session.RevokedAtUtc is null)
        {
            session.IsActive = false;
            session.RevokedAtUtc = DateTime.UtcNow;
            session.RevocationReason = reason;
        }
        return Task.CompletedTask;
    }

    public Task RevokeByUserApplicationAsync(long userId, long applicationId, string reason, CancellationToken ct)
    {
        foreach (var session in _sessions.Values.Where(s => s.UserId == userId && s.ApplicationId == applicationId && s.RevokedAtUtc is null))
        {
            session.IsActive = false;
            session.RevokedAtUtc = DateTime.UtcNow;
            session.RevocationReason = reason;
        }
        return Task.CompletedTask;
    }

    public Task RevokeAllByUserAsync(long userId, string reason, CancellationToken ct)
    {
        foreach (var session in _sessions.Values.Where(s => s.UserId == userId && s.RevokedAtUtc is null))
        {
            session.IsActive = false;
            session.RevokedAtUtc = DateTime.UtcNow;
            session.RevocationReason = reason;
        }
        return Task.CompletedTask;
    }
}
