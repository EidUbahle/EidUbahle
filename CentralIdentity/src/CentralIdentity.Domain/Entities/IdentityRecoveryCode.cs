namespace CentralIdentity.Domain.Entities;

public sealed class IdentityRecoveryCode
{
    public long RecoveryCodeId { get; set; }
    public long UserId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public bool IsUsed { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
