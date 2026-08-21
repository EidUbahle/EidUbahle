using CentralIdentity.Application.Services;
using CentralIdentity.Contracts.Common;
using CentralIdentity.Contracts.UserApplications;
using CentralIdentity.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CentralIdentity.Api.Controllers;

/// <summary>
/// Manages which applications a given user is granted access to.
/// </summary>
[Route("api/users/{userId:long}/applications")]
public sealed class UserApplicationsController : ApiControllerBase
{
    private readonly IUserApplicationService _userApplicationService;

    public UserApplicationsController(IUserApplicationService userApplicationService)
    {
        _userApplicationService = userApplicationService;
    }

    /// <summary>Assigns a user to an application.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserApplicationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<UserApplicationResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Assign(long userId, [FromBody] AssignUserApplicationRequest request, CancellationToken ct)
    {
        var result = await _userApplicationService.AssignUserToApplicationAsync(userId, request.ApplicationId, ct);
        if (result.IsFailure)
            return BadRequest(ApiResponse<UserApplicationResponse>.Fail(result.Error!));

        var assignments = await _userApplicationService.GetUserApplicationsAsync(userId, ct);
        var created = assignments.Value.First(a => a.ApplicationId == request.ApplicationId);
        return CreatedAtAction(nameof(GetUserApplications), new { userId }, ApiResponse<UserApplicationResponse>.Ok(ToResponse(created)));
    }

    /// <summary>Lists the applications a user has been assigned to.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserApplicationResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserApplications(long userId, CancellationToken ct)
    {
        var result = await _userApplicationService.GetUserApplicationsAsync(userId, ct);
        var response = result.Value.Select(ToResponse).ToList();
        return Ok(ApiResponse<IReadOnlyList<UserApplicationResponse>>.Ok(response));
    }

    /// <summary>Revokes a user's access to an application (independent of their access to other applications).</summary>
    [HttpPost("{applicationId:long}/revoke")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Revoke(long userId, long applicationId, [FromBody] RevokeUserApplicationRequest request, CancellationToken ct)
    {
        var result = await _userApplicationService.RevokeUserApplicationAsync(userId, applicationId, request.Reason, ct);
        if (result.IsFailure)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse.Ok());
    }

    private static UserApplicationResponse ToResponse(IdentityUserApplication ua) => new()
    {
        UserApplicationId = ua.UserApplicationId,
        UserId = ua.UserId,
        ApplicationId = ua.ApplicationId,
        IsActive = ua.IsActive,
        AssignedAtUtc = ua.AssignedAtUtc,
        LastAccessAtUtc = ua.LastAccessAtUtc,
        LastActivityAtUtc = ua.LastActivityAtUtc,
        RevokedAtUtc = ua.RevokedAtUtc,
        RevocationReason = ua.RevocationReason
    };
}
