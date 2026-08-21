using CentralIdentity.Domain.Entities;

namespace CentralIdentity.Application.Common.Interfaces;

public interface IUserRoleRepository
{
    Task<IReadOnlyList<IdentityUserRole>> GetActiveByUserApplicationAsync(long userId, long applicationId, CancellationToken ct);
    Task AssignAsync(IdentityUserRole userRole, CancellationToken ct);
    Task RevokeAsync(long userId, long applicationId, long roleId, CancellationToken ct);
    Task<IReadOnlyList<IdentityPermission>> GetEffectivePermissionsAsync(long userId, long applicationId, CancellationToken ct);
}
