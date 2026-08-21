namespace CentralIdentity.Domain.Entities;

public sealed class IdentityApplication
{
    public long ApplicationId { get; set; }
    public string ApplicationCode { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecretHash { get; set; }
    public string ClientType { get; set; } = "Confidential";
    public string Audience { get; set; } = string.Empty;
    public string? AllowedRedirectUris { get; set; }
    public string? AllowedOrigins { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public List<string> GetRedirectUris() =>
        string.IsNullOrWhiteSpace(AllowedRedirectUris)
            ? new List<string>()
            : AllowedRedirectUris.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    public List<string> GetAllowedOrigins() =>
        string.IsNullOrWhiteSpace(AllowedOrigins)
            ? new List<string>()
            : AllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
