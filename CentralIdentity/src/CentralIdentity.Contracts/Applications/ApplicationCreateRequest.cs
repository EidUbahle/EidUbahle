using System.ComponentModel.DataAnnotations;

namespace CentralIdentity.Contracts.Applications;

public sealed class ApplicationCreateRequest
{
    [Required, StringLength(50, MinimumLength = 2)]
    public string ApplicationCode { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 2)]
    public string ApplicationName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>"Confidential" or "Public".</summary>
    [Required]
    public string ClientType { get; set; } = "Confidential";

    [Required, StringLength(200)]
    public string Audience { get; set; } = string.Empty;

    /// <summary>Comma-separated list of allowed redirect URIs.</summary>
    public string? AllowedRedirectUris { get; set; }

    /// <summary>Comma-separated list of allowed CORS origins.</summary>
    public string? AllowedOrigins { get; set; }
}
