namespace CentralIdentity.Contracts.Applications;

/// <summary>
/// Public-facing application representation. Never includes ClientSecretHash.
/// </summary>
public sealed class ApplicationResponse
{
    public long ApplicationId { get; set; }
    public string ApplicationCode { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientType { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string? AllowedRedirectUris { get; set; }
    public string? AllowedOrigins { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
