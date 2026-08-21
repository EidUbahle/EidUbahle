namespace CentralIdentity.Domain.Entities;

public sealed class IdentitySession
{
    public Guid SessionId { get; set; }
    public long UserId { get; set; }
    public long ApplicationId { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastActivityAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevocationReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceId { get; set; }
    public string SecurityStamp { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
