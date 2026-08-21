using System.ComponentModel.DataAnnotations;

namespace CentralIdentity.Contracts.Users;

public sealed class UserUpdateRequest
{
    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }
}
