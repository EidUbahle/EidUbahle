namespace CentralIdentity.Contracts.UserApplications;

public sealed class UserApplicationResponse
{
    public long UserApplicationId { get; set; }
    public long UserId { get; set; }
    public long ApplicationId { get; set; }
    public bool IsActive { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public DateTime? LastAccessAtUtc { get; set; }
    public DateTime? LastActivityAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevocationReason { get; set; }
}
