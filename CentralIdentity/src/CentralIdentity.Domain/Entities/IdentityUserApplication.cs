namespace CentralIdentity.Domain.Entities;

public sealed class IdentityUserApplication
{
    public long UserApplicationId { get; set; }
    public long UserId { get; set; }
    public long ApplicationId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Status { get; set; } = "Active";
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessAtUtc { get; set; }
    public DateTime? LastActivityAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevocationReason { get; set; }
    public string SecurityStamp { get; set; } = string.Empty;
}
