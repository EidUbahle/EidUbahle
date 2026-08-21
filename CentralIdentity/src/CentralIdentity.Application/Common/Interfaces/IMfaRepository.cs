using CentralIdentity.Domain.Entities;

namespace CentralIdentity.Application.Common.Interfaces;

public interface IMfaRepository
{
    Task<IdentityMfaMethod?> GetByUserAndTypeAsync(long userId, string methodType, CancellationToken ct);
    Task<IReadOnlyList<IdentityMfaMethod>> GetByUserAsync(long userId, CancellationToken ct);
    Task CreateOrUpdateAsync(IdentityMfaMethod method, CancellationToken ct);
    Task<IReadOnlyList<IdentityRecoveryCode>> GetActiveRecoveryCodesAsync(long userId, CancellationToken ct);
    Task SaveRecoveryCodesAsync(long userId, IEnumerable<IdentityRecoveryCode> codes, CancellationToken ct);
    Task MarkRecoveryCodeUsedAsync(long recoveryCodeId, CancellationToken ct);
}
