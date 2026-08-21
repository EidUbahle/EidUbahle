namespace CentralIdentity.Domain.Entities;

public sealed class IdentityMfaMethod
{
    public long MfaMethodId { get; set; }
    public long UserId { get; set; }
    public string MethodType { get; set; } = "TOTP";
    public string SecretEncrypted { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EnabledAtUtc { get; set; }
    public DateTime? DisabledAtUtc { get; set; }
}
