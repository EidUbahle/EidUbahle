using CentralIdentity.Application.Services;
using CentralIdentity.Contracts.Applications;
using CentralIdentity.Contracts.Common;
using CentralIdentity.Contracts.UserApplications;
using CentralIdentity.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CentralIdentity.Api.Controllers;

/// <summary>
/// OAuth client application registration and management endpoints.
/// </summary>
[Route("api/applications")]
public sealed class ApplicationsController : ApiControllerBase
{
    private readonly IApplicationService _applicationService;
    private readonly IUserApplicationService _userApplicationService;

    public ApplicationsController(IApplicationService applicationService, IUserApplicationService userApplicationService)
    {
        _applicationService = applicationService;
        _userApplicationService = userApplicationService;
    }

    /// <summary>Registers a new client application. The plaintext client secret is returned only once.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ApplicationCreateResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ApplicationCreateResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] ApplicationCreateRequest request, CancellationToken ct)
    {
        var result = await _applicationService.RegisterApplicationAsync(new RegisterApplicationCommand(
            request.ApplicationCode, request.ApplicationName, request.Description, request.ClientType,
            request.Audience, request.AllowedRedirectUris, request.AllowedOrigins), ct);

        if (result.IsFailure)
            return BadRequest(ApiResponse<ApplicationCreateResponse>.Fail(result.Error!));

        var response = new ApplicationCreateResponse
        {
            ApplicationId = result.Value.ApplicationId,
            ApplicationCode = result.Value.ApplicationCode,
            ClientId = result.Value.ClientId,
            PlaintextClientSecret = result.Value.PlaintextClientSecret,
            ClientType = result.Value.ClientType,
            Audience = result.Value.Audience
        };
        return CreatedAtAction(nameof(GetById), new { id = response.ApplicationId }, ApiResponse<ApplicationCreateResponse>.Ok(response));
    }

    /// <summary>Gets a paged list of registered applications.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ApplicationResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _applicationService.GetApplicationsAsync(page, pageSize, ct);
        var response = result.Value.Select(ToResponse).ToList();
        return Ok(ApiResponse<IReadOnlyList<ApplicationResponse>>.Ok(response));
    }

    /// <summary>Gets a single application by ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ApplicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ApplicationResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _applicationService.GetApplicationAsync(id, ct);
        if (result.IsFailure)
            return NotFound(ApiResponse<ApplicationResponse>.Fail(result.Error!));
        return Ok(ApiResponse<ApplicationResponse>.Ok(ToResponse(result.Value)));
    }

    /// <summary>Updates application metadata.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(long id, [FromBody] ApplicationUpdateRequest request, CancellationToken ct)
    {
        var result = await _applicationService.UpdateApplicationAsync(new UpdateApplicationCommand(
            id, request.ApplicationName, request.Description, request.AllowedRedirectUris, request.AllowedOrigins), ct);
        if (result.IsFailure)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse.Ok());
    }

    /// <summary>Re-activates a disabled application.</summary>
    [HttpPost("{id:long}/enable")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Enable(long id, CancellationToken ct)
    {
        var result = await _applicationService.EnableApplicationAsync(id, ct);
        if (result.IsFailure)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse.Ok());
    }

    /// <summary>Deactivates an application, preventing further token issuance for it.</summary>
    [HttpPost("{id:long}/disable")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Disable(long id, CancellationToken ct)
    {
        var result = await _applicationService.DisableApplicationAsync(id, ct);
        if (result.IsFailure)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse.Ok());
    }

    /// <summary>Lists the users assigned to this application.</summary>
    [HttpGet("{applicationId:long}/users")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserApplicationResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplicationUsers(long applicationId, CancellationToken ct)
    {
        var result = await _userApplicationService.GetApplicationUsersAsync(applicationId, ct);
        var response = result.Value.Select(ToResponse).ToList();
        return Ok(ApiResponse<IReadOnlyList<UserApplicationResponse>>.Ok(response));
    }

    private static ApplicationResponse ToResponse(IdentityApplication app) => new()
    {
        ApplicationId = app.ApplicationId,
        ApplicationCode = app.ApplicationCode,
        ApplicationName = app.ApplicationName,
        Description = app.Description,
        ClientId = app.ClientId,
        ClientType = app.ClientType,
        Audience = app.Audience,
        AllowedRedirectUris = app.AllowedRedirectUris,
        AllowedOrigins = app.AllowedOrigins,
        IsActive = app.IsActive,
        CreatedAtUtc = app.CreatedAtUtc,
        UpdatedAtUtc = app.UpdatedAtUtc
    };

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
