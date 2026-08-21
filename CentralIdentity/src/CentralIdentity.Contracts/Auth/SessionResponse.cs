namespace CentralIdentity.Contracts.Auth;

public sealed class SessionResponse
{
    public Guid SessionId { get; set; }
    public long UserId { get; set; }
    public long ApplicationId { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastActivityAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceId { get; set; }
    public bool IsActive { get; set; }
}
