using System.ComponentModel.DataAnnotations;

namespace CentralIdentity.Contracts.Applications;

public sealed class ApplicationUpdateRequest
{
    [StringLength(200)]
    public string? ApplicationName { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public string? AllowedRedirectUris { get; set; }

    public string? AllowedOrigins { get; set; }
}
