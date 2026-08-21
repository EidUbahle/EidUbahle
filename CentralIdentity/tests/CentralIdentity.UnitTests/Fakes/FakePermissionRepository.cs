using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;

namespace CentralIdentity.UnitTests.Fakes;

public sealed class FakePermissionRepository : IPermissionRepository
{
    private readonly List<IdentityPermission> _permissions = new();
    private long _nextId = 1;

    public Task<IdentityPermission?> GetByIdAsync(long permissionId, CancellationToken ct)
        => Task.FromResult(_permissions.FirstOrDefault(p => p.PermissionId == permissionId));

    public Task<IReadOnlyList<IdentityPermission>> GetByApplicationAsync(long applicationId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IdentityPermission>>(_permissions.Where(p => p.ApplicationId == applicationId).ToList());

    public Task<long> CreateAsync(IdentityPermission permission, CancellationToken ct)
    {
        permission.PermissionId = _nextId++;
        _permissions.Add(permission);
        return Task.FromResult(permission.PermissionId);
    }

    public Task UpdateAsync(IdentityPermission permission, CancellationToken ct)
    {
        var idx = _permissions.FindIndex(p => p.PermissionId == permission.PermissionId);
        if (idx >= 0) _permissions[idx] = permission;
        return Task.CompletedTask;
    }
}
