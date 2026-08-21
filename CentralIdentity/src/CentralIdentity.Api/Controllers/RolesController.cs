using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Contracts.Permissions;
using CentralIdentity.Contracts.Roles;
using CentralIdentity.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentralIdentity.Api.Controllers;

[ApiController]
[Authorize(Roles = "admin,administrator")]
public sealed class RolesController : ApiControllerBase
{
    private readonly IRoleRepository _roleRepo;
    private readonly IPermissionRepository _permRepo;
    private readonly IAuditLogRepository _auditLog;

    public RolesController(IRoleRepository roleRepo, IPermissionRepository permRepo, IAuditLogRepository auditLog)
    {
        _roleRepo = roleRepo;
        _permRepo = permRepo;
        _auditLog = auditLog;
    }

    [HttpGet("/api/applications/{applicationId:long}/roles")]
    public async Task<IActionResult> GetRoles(long applicationId, CancellationToken ct)
    {
        var roles = await _roleRepo.GetByApplicationAsync(applicationId, ct);
        var response = roles.Select(r => new RoleResponse
        {
            RoleId = r.RoleId,
            ApplicationId = r.ApplicationId,
            RoleCode = r.RoleCode,
            RoleName = r.RoleName,
            Description = r.Description,
            IsActive = r.IsActive,
            CreatedAtUtc = r.CreatedAtUtc
        });
        return Ok(response);
    }

    [HttpPost("/api/applications/{applicationId:long}/roles")]
    public async Task<IActionResult> CreateRole(long applicationId, [FromBody] RoleCreateRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RoleCode) || string.IsNullOrWhiteSpace(request.RoleName))
            return BadRequest("RoleCode and RoleName are required.");

        var role = new IdentityRole
        {
            ApplicationId = applicationId,
            RoleCode = request.RoleCode.Trim(),
            RoleName = request.RoleName.Trim(),
            Description = request.Description,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        var id = await _roleRepo.CreateAsync(role, ct);
        role.RoleId = id;

        return CreatedAtAction(
            nameof(GetRoles),
            new { applicationId },
            new RoleResponse
            {
                RoleId = id,
                ApplicationId = applicationId,
                RoleCode = role.RoleCode,
                RoleName = role.RoleName,
                Description = role.Description,
                IsActive = true,
                CreatedAtUtc = role.CreatedAtUtc
            });
    }

    [HttpPut("/api/roles/{roleId:long}")]
    public async Task<IActionResult> UpdateRole(long roleId, [FromBody] RoleUpdateRequest request, CancellationToken ct)
    {
        var role = await _roleRepo.GetByIdAsync(roleId, ct);
        if (role == null) return NotFound();
        role.RoleName = request.RoleName.Trim();
        role.Description = request.Description;
        role.UpdatedAtUtc = DateTime.UtcNow;
        await _roleRepo.UpdateAsync(role, ct);
        return NoContent();
    }

    [HttpPost("/api/roles/{roleId:long}/enable")]
    public async Task<IActionResult> EnableRole(long roleId, CancellationToken ct)
    {
        var role = await _roleRepo.GetByIdAsync(roleId, ct);
        if (role == null) return NotFound();
        role.IsActive = true;
        role.UpdatedAtUtc = DateTime.UtcNow;
        await _roleRepo.UpdateAsync(role, ct);
        return NoContent();
    }

    [HttpPost("/api/roles/{roleId:long}/disable")]
    public async Task<IActionResult> DisableRole(long roleId, CancellationToken ct)
    {
        var role = await _roleRepo.GetByIdAsync(roleId, ct);
        if (role == null) return NotFound();
        role.IsActive = false;
        role.UpdatedAtUtc = DateTime.UtcNow;
        await _roleRepo.UpdateAsync(role, ct);
        return NoContent();
    }

    [HttpPost("/api/roles/{roleId:long}/permissions")]
    public async Task<IActionResult> AssignPermission(long roleId, [FromBody] AssignPermissionRequest request, CancellationToken ct)
    {
        var role = await _roleRepo.GetByIdAsync(roleId, ct);
        if (role == null) return NotFound("Role not found.");

        var permission = await _permRepo.GetByIdAsync(request.PermissionId, ct);
        if (permission == null) return NotFound("Permission not found.");

        if (role.ApplicationId != permission.ApplicationId)
            return BadRequest("Permission does not belong to the same application as the role.");

        await _roleRepo.AssignPermissionAsync(roleId, request.PermissionId, ct);
        return NoContent();
    }
}
