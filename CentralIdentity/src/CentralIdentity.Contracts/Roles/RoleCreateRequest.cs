namespace CentralIdentity.Contracts.Roles;

public sealed class RoleCreateRequest
{
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
