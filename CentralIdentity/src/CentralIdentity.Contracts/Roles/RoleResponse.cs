namespace CentralIdentity.Contracts.Roles;

public sealed class RoleResponse
{
    public long RoleId { get; set; }
    public long ApplicationId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
