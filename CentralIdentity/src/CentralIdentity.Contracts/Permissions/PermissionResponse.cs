namespace CentralIdentity.Contracts.Permissions;

public sealed class PermissionResponse
{
    public long PermissionId { get; set; }
    public long ApplicationId { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public string PermissionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
