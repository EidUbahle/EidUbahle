using CentralIdentity.Application.Services;
using CentralIdentity.Contracts.Common;
using CentralIdentity.Contracts.Users;
using CentralIdentity.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CentralIdentity.Api.Controllers;

/// <summary>
/// User management endpoints.
/// </summary>
[Route("api/users")]
public sealed class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>Creates a new user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] UserCreateRequest request, CancellationToken ct)
    {
        var result = await _userService.CreateUserAsync(new CreateUserCommand(
            request.Username, request.Email, request.Phone, request.Password, request.FirstName, request.LastName), ct);

        if (result.IsFailure)
            return BadRequest(ApiResponse<UserResponse>.Fail(result.Error!));

        var userResult = await _userService.GetUserAsync(result.Value, ct);
        var response = ToResponse(userResult.Value);
        return CreatedAtAction(nameof(GetById), new { id = response.UserId }, ApiResponse<UserResponse>.Ok(response));
    }

    /// <summary>Gets a paged list of users.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _userService.GetUsersAsync(page, pageSize, ct);
        var response = result.Value.Select(ToResponse).ToList();
        return Ok(ApiResponse<IReadOnlyList<UserResponse>>.Ok(response));
    }

    /// <summary>Gets a single user by ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _userService.GetUserAsync(id, ct);
        if (result.IsFailure)
            return NotFound(ApiResponse<UserResponse>.Fail(result.Error!));
        return Ok(ApiResponse<UserResponse>.Ok(ToResponse(result.Value)));
    }

    /// <summary>Updates a user's profile fields.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(long id, [FromBody] UserUpdateRequest request, CancellationToken ct)
    {
        var result = await _userService.UpdateUserAsync(new UpdateUserCommand(id, request.Phone, request.FirstName, request.LastName), ct);
        if (result.IsFailure)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse.Ok());
    }

    /// <summary>Re-activates a disabled user.</summary>
    [HttpPost("{id:long}/enable")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Enable(long id, CancellationToken ct)
    {
        var result = await _userService.EnableUserAsync(id, ct);
        if (result.IsFailure)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse.Ok());
    }

    /// <summary>Deactivates a user, preventing further authentication.</summary>
    [HttpPost("{id:long}/disable")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Disable(long id, CancellationToken ct)
    {
        var result = await _userService.DisableUserAsync(id, ct);
        if (result.IsFailure)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse.Ok());
    }

    private static UserResponse ToResponse(IdentityUser user) => new()
    {
        UserId = user.UserId,
        Username = user.Username,
        Email = user.Email,
        Phone = user.Phone,
        FirstName = user.FirstName,
        LastName = user.LastName,
        IsActive = user.IsActive,
        EmailVerified = user.EmailVerified,
        PhoneVerified = user.PhoneVerified,
        TwoFactorEnabled = user.TwoFactorEnabled,
        LastLoginAtUtc = user.LastLoginAtUtc,
        CreatedAtUtc = user.CreatedAtUtc,
        UpdatedAtUtc = user.UpdatedAtUtc
    };
}
