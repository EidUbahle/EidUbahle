using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Contracts.Permissions;
using CentralIdentity.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentralIdentity.Api.Controllers;

[ApiController]
[Authorize(Roles = "admin,administrator")]
public sealed class PermissionsController : ApiControllerBase
{
    private readonly IPermissionRepository _permRepo;

    public PermissionsController(IPermissionRepository permRepo) => _permRepo = permRepo;

    [HttpGet("/api/applications/{applicationId:long}/permissions")]
    public async Task<IActionResult> GetPermissions(long applicationId, CancellationToken ct)
    {
        var perms = await _permRepo.GetByApplicationAsync(applicationId, ct);
        var response = perms.Select(p => new PermissionResponse
        {
            PermissionId = p.PermissionId,
            ApplicationId = p.ApplicationId,
            PermissionCode = p.PermissionCode,
            PermissionName = p.PermissionName,
            Description = p.Description,
            IsActive = p.IsActive,
            CreatedAtUtc = p.CreatedAtUtc
        });
        return Ok(response);
    }

    [HttpPost("/api/applications/{applicationId:long}/permissions")]
    public async Task<IActionResult> CreatePermission(long applicationId, [FromBody] PermissionCreateRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.PermissionCode) || string.IsNullOrWhiteSpace(request.PermissionName))
            return BadRequest("PermissionCode and PermissionName are required.");

        var permission = new IdentityPermission
        {
            ApplicationId = applicationId,
            PermissionCode = request.PermissionCode.Trim(),
            PermissionName = request.PermissionName.Trim(),
            Description = request.Description,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        var id = await _permRepo.CreateAsync(permission, ct);

        return CreatedAtAction(
            nameof(GetPermissions),
            new { applicationId },
            new PermissionResponse
            {
                PermissionId = id,
                ApplicationId = applicationId,
                PermissionCode = permission.PermissionCode,
                PermissionName = permission.PermissionName,
                IsActive = true,
                CreatedAtUtc = permission.CreatedAtUtc
            });
    }
}
