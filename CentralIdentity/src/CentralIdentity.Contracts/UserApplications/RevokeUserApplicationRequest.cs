using System.ComponentModel.DataAnnotations;

namespace CentralIdentity.Contracts.UserApplications;

public sealed class RevokeUserApplicationRequest
{
    [StringLength(500)]
    public string? Reason { get; set; }
}
