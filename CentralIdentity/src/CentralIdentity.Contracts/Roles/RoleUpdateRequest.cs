namespace CentralIdentity.Contracts.Roles;

public sealed class RoleUpdateRequest
{
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
