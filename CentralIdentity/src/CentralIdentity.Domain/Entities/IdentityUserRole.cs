namespace CentralIdentity.Domain.Entities;

public sealed class IdentityUserRole
{
    public long UserRoleId { get; set; }
    public long UserId { get; set; }
    public long ApplicationId { get; set; }
    public long RoleId { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
}
