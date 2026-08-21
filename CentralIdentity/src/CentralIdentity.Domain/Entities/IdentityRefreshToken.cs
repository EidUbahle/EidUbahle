namespace CentralIdentity.Domain.Entities;

public sealed class IdentityRefreshToken
{
    public Guid RefreshTokenId { get; set; }
    public long UserId { get; set; }
    public long ApplicationId { get; set; }
    public Guid SessionId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public string? RevocationReason { get; set; }
    public Guid TokenFamilyId { get; set; }
    public string? CreatedIpAddress { get; set; }
    public string? LastUsedIpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string Scope { get; set; } = string.Empty;
}
