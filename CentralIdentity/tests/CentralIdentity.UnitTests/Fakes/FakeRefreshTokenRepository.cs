using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;

namespace CentralIdentity.UnitTests.Fakes;

public sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly Dictionary<Guid, IdentityRefreshToken> _tokens = new();

    public IReadOnlyCollection<IdentityRefreshToken> Tokens => _tokens.Values.ToList().AsReadOnly();

    public Task<IdentityRefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct) =>
        Task.FromResult(_tokens.Values.FirstOrDefault(t => string.Equals(t.TokenHash, tokenHash, StringComparison.OrdinalIgnoreCase)));

    public Task CreateAsync(IdentityRefreshToken token, CancellationToken ct)
    {
        _tokens[token.RefreshTokenId] = token;
        return Task.CompletedTask;
    }

    public Task RevokeAsync(Guid refreshTokenId, string reason, CancellationToken ct)
    {
        if (_tokens.TryGetValue(refreshTokenId, out var token) && token.RevokedAtUtc is null)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            token.RevocationReason = reason;
        }

        return Task.CompletedTask;
    }

    public Task RevokeByFamilyAsync(Guid familyId, string reason, CancellationToken ct)
    {
        foreach (var token in _tokens.Values.Where(t => t.TokenFamilyId == familyId && t.RevokedAtUtc is null))
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            token.RevocationReason = reason;
        }

        return Task.CompletedTask;
    }

    public Task RevokeBySessionAsync(Guid sessionId, string reason, CancellationToken ct)
    {
        foreach (var token in _tokens.Values.Where(t => t.SessionId == sessionId && t.RevokedAtUtc is null))
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            token.RevocationReason = reason;
        }

        return Task.CompletedTask;
    }

    public Task RevokeByUserApplicationAsync(long userId, long applicationId, string reason, CancellationToken ct)
    {
        foreach (var token in _tokens.Values.Where(t => t.UserId == userId && t.ApplicationId == applicationId && t.RevokedAtUtc is null))
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            token.RevocationReason = reason;
        }

        return Task.CompletedTask;
    }

    public Task RevokeAllByUserAsync(long userId, string reason, CancellationToken ct)
    {
        foreach (var token in _tokens.Values.Where(t => t.UserId == userId && t.RevokedAtUtc is null))
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            token.RevocationReason = reason;
        }

        return Task.CompletedTask;
    }
}
