namespace CentralIdentity.Domain.Entities;

public sealed class AuthorizationCode
{
    public long CodeId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public long UserId { get; set; }
    public long ApplicationId { get; set; }
    public string RedirectUri { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
}
