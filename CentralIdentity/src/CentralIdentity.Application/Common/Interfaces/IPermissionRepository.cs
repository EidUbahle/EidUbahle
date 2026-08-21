using CentralIdentity.Domain.Entities;

namespace CentralIdentity.Application.Common.Interfaces;

public interface IPermissionRepository
{
    Task<IdentityPermission?> GetByIdAsync(long permissionId, CancellationToken ct);
    Task<IReadOnlyList<IdentityPermission>> GetByApplicationAsync(long applicationId, CancellationToken ct);
    Task<long> CreateAsync(IdentityPermission permission, CancellationToken ct);
    Task UpdateAsync(IdentityPermission permission, CancellationToken ct);
}
