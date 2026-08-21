namespace CentralIdentity.Domain.Entities;

public sealed class IdentityRolePermission
{
    public long RolePermissionId { get; set; }
    public long RoleId { get; set; }
    public long PermissionId { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
}
