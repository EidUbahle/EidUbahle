using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;

namespace CentralIdentity.IntegrationTests.Fakes;

public sealed class FakeUserRoleRepository : IUserRoleRepository
{
    private readonly List<IdentityUserRole> _userRoles = new();
    private readonly List<IdentityPermission> _effectivePermissions = new();
    private long _nextId = 1;

    public Task<IReadOnlyList<IdentityUserRole>> GetActiveByUserApplicationAsync(long userId, long applicationId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IdentityUserRole>>(
            _userRoles.Where(ur => ur.UserId == userId && ur.ApplicationId == applicationId && ur.IsActive).ToList());

    public Task AssignAsync(IdentityUserRole userRole, CancellationToken ct)
    {
        userRole.UserRoleId = _nextId++;
        _userRoles.Add(userRole);
        return Task.CompletedTask;
    }

    public Task RevokeAsync(long userId, long applicationId, long roleId, CancellationToken ct)
    {
        var ur = _userRoles.FirstOrDefault(u => u.UserId == userId && u.ApplicationId == applicationId && u.RoleId == roleId && u.IsActive);
        if (ur != null)
        {
            ur.IsActive = false;
            ur.RevokedAtUtc = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<IdentityPermission>> GetEffectivePermissionsAsync(long userId, long applicationId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IdentityPermission>>(_effectivePermissions.Where(p => p.ApplicationId == applicationId).ToList());

    public void AddEffectivePermission(IdentityPermission permission) => _effectivePermissions.Add(permission);
    public List<IdentityUserRole> UserRoles => _userRoles;
}
