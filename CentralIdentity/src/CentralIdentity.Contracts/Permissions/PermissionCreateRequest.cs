namespace CentralIdentity.Contracts.Permissions;

public sealed class PermissionCreateRequest
{
    public string PermissionCode { get; set; } = string.Empty;
    public string PermissionName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
