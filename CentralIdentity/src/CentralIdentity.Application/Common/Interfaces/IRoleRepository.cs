using CentralIdentity.Domain.Entities;

namespace CentralIdentity.Application.Common.Interfaces;

public interface IRoleRepository
{
    Task<IdentityRole?> GetByIdAsync(long roleId, CancellationToken ct);
    Task<IReadOnlyList<IdentityRole>> GetByApplicationAsync(long applicationId, CancellationToken ct);
    Task<long> CreateAsync(IdentityRole role, CancellationToken ct);
    Task UpdateAsync(IdentityRole role, CancellationToken ct);
    Task<IReadOnlyList<IdentityPermission>> GetPermissionsAsync(long roleId, CancellationToken ct);
    Task AssignPermissionAsync(long roleId, long permissionId, CancellationToken ct);
    Task RemovePermissionAsync(long roleId, long permissionId, CancellationToken ct);
}
