using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;

namespace CentralIdentity.UnitTests.Fakes;

public sealed class FakeRoleRepository : IRoleRepository
{
    private readonly List<IdentityRole> _roles = new();
    private readonly List<IdentityRolePermission> _rolePermissions = new();
    private long _nextId = 1;

    public Task<IdentityRole?> GetByIdAsync(long roleId, CancellationToken ct)
        => Task.FromResult(_roles.FirstOrDefault(r => r.RoleId == roleId));

    public Task<IReadOnlyList<IdentityRole>> GetByApplicationAsync(long applicationId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IdentityRole>>(_roles.Where(r => r.ApplicationId == applicationId).ToList());

    public Task<long> CreateAsync(IdentityRole role, CancellationToken ct)
    {
        role.RoleId = _nextId++;
        _roles.Add(role);
        return Task.FromResult(role.RoleId);
    }

    public Task UpdateAsync(IdentityRole role, CancellationToken ct)
    {
        var idx = _roles.FindIndex(r => r.RoleId == role.RoleId);
        if (idx >= 0) _roles[idx] = role;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<IdentityPermission>> GetPermissionsAsync(long roleId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IdentityPermission>>(new List<IdentityPermission>());

    public Task AssignPermissionAsync(long roleId, long permissionId, CancellationToken ct)
    {
        if (!_rolePermissions.Any(rp => rp.RoleId == roleId && rp.PermissionId == permissionId))
            _rolePermissions.Add(new IdentityRolePermission { RoleId = roleId, PermissionId = permissionId, AssignedAtUtc = DateTime.UtcNow });
        return Task.CompletedTask;
    }

    public Task RemovePermissionAsync(long roleId, long permissionId, CancellationToken ct)
    {
        _rolePermissions.RemoveAll(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
        return Task.CompletedTask;
    }

    public List<IdentityRolePermission> RolePermissions => _rolePermissions;
}
