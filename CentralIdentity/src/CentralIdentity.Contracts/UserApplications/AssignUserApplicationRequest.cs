using System.ComponentModel.DataAnnotations;

namespace CentralIdentity.Contracts.UserApplications;

public sealed class AssignUserApplicationRequest
{
    [Required]
    public long ApplicationId { get; set; }
}
