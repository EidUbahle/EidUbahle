using CentralIdentity.Domain.Entities;

namespace CentralIdentity.Application.Common.Interfaces;

public interface ISessionRepository
{
    Task<IdentitySession?> GetByIdAsync(Guid sessionId, CancellationToken ct);
    Task<IReadOnlyList<IdentitySession>> GetActiveByUserAsync(long userId, CancellationToken ct);
    Task<IReadOnlyList<IdentitySession>> GetActiveByUserApplicationAsync(long userId, long applicationId, CancellationToken ct);
    Task CreateAsync(IdentitySession session, CancellationToken ct);
    Task UpdateActivityAsync(Guid sessionId, DateTime lastActivityAtUtc, CancellationToken ct);
    Task RevokeAsync(Guid sessionId, string reason, CancellationToken ct);
    Task RevokeByUserApplicationAsync(long userId, long applicationId, string reason, CancellationToken ct);
    Task RevokeAllByUserAsync(long userId, string reason, CancellationToken ct);
}
