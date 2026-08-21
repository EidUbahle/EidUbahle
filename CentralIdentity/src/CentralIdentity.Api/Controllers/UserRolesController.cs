using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Contracts.UserRoles;
using CentralIdentity.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentralIdentity.Api.Controllers;

[ApiController]
[Authorize]
public sealed class UserRolesController : ApiControllerBase
{
    private readonly IUserRoleRepository _userRoleRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IUserApplicationRepository _userAppRepo;
    private readonly IAuditLogRepository _auditLog;

    public UserRolesController(IUserRoleRepository userRoleRepo, IRoleRepository roleRepo, IUserApplicationRepository userAppRepo, IAuditLogRepository auditLog)
    {
        _userRoleRepo = userRoleRepo;
        _roleRepo = roleRepo;
        _userAppRepo = userAppRepo;
        _auditLog = auditLog;
    }

    [HttpGet("/api/users/{userId:long}/applications/{applicationId:long}/roles")]
    public async Task<IActionResult> GetUserRoles(long userId, long applicationId, CancellationToken ct)
    {
        var userRoles = await _userRoleRepo.GetActiveByUserApplicationAsync(userId, applicationId, ct);
        return Ok(userRoles.Select(ur => new { ur.UserRoleId, ur.RoleId, ur.ApplicationId, ur.AssignedAtUtc }));
    }

    [HttpPost("/api/users/{userId:long}/applications/{applicationId:long}/roles")]
    public async Task<IActionResult> AssignRole(long userId, long applicationId, [FromBody] AssignUserRoleRequest request, CancellationToken ct)
    {
        var userApp = await _userAppRepo.GetAsync(userId, applicationId, ct);
        if (userApp == null || !userApp.IsActive)
            return BadRequest("User does not have active access to this application.");

        var role = await _roleRepo.GetByIdAsync(request.RoleId, ct);
        if (role == null)
            return NotFound("Role not found.");
        if (role.ApplicationId != applicationId)
            return BadRequest("Role does not belong to this application.");
        if (!role.IsActive)
            return BadRequest("Role is disabled.");

        await _userRoleRepo.AssignAsync(new IdentityUserRole
        {
            UserId = userId,
            ApplicationId = applicationId,
            RoleId = request.RoleId,
            AssignedAtUtc = DateTime.UtcNow,
            IsActive = true
        }, ct);

        return NoContent();
    }

    [HttpDelete("/api/users/{userId:long}/applications/{applicationId:long}/roles/{roleId:long}")]
    public async Task<IActionResult> RevokeRole(long userId, long applicationId, long roleId, CancellationToken ct)
    {
        await _userRoleRepo.RevokeAsync(userId, applicationId, roleId, ct);
        return NoContent();
    }
}
