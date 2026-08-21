using CentralIdentity.Domain.Entities;

namespace CentralIdentity.Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    Task<IdentityRefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct);
    Task CreateAsync(IdentityRefreshToken token, CancellationToken ct);
    Task RevokeAsync(Guid refreshTokenId, string reason, CancellationToken ct);
    Task RevokeByFamilyAsync(Guid familyId, string reason, CancellationToken ct);
    Task RevokeBySessionAsync(Guid sessionId, string reason, CancellationToken ct);
    Task RevokeByUserApplicationAsync(long userId, long applicationId, string reason, CancellationToken ct);
    Task RevokeAllByUserAsync(long userId, string reason, CancellationToken ct);
}
