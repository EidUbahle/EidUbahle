using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Common;
using CentralIdentity.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CentralIdentity.Application.Services;

public interface IUserService
{
    Task<Result<long>> CreateUserAsync(CreateUserCommand command, CancellationToken ct = default);
    Task<Result<IdentityUser>> GetUserAsync(long userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<IdentityUser>>> GetUsersAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result> UpdateUserAsync(UpdateUserCommand command, CancellationToken ct = default);
    Task<Result> EnableUserAsync(long userId, CancellationToken ct = default);
    Task<Result> DisableUserAsync(long userId, CancellationToken ct = default);
}

public sealed record CreateUserCommand(
    string Username,
    string Email,
    string? Phone,
    string Password,
    string FirstName,
    string LastName);

public sealed record UpdateUserCommand(
    long UserId,
    string? Phone,
    string? FirstName,
    string? LastName);

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepo, IPasswordHasher passwordHasher, ILogger<UserService> logger)
    {
        _userRepo = userRepo;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result<long>> CreateUserAsync(CreateUserCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Username)) return Result.Failure<long>("Username is required.");
        if (string.IsNullOrWhiteSpace(command.Email)) return Result.Failure<long>("Email is required.");
        if (string.IsNullOrWhiteSpace(command.Password)) return Result.Failure<long>("Password is required.");
        if (string.IsNullOrWhiteSpace(command.FirstName)) return Result.Failure<long>("FirstName is required.");
        if (string.IsNullOrWhiteSpace(command.LastName)) return Result.Failure<long>("LastName is required.");

        if (await _userRepo.ExistsByEmailAsync(command.Email, ct))
            return Result.Failure<long>("Email already exists.");

        if (await _userRepo.ExistsByUsernameAsync(command.Username, ct))
            return Result.Failure<long>("Username already exists.");

        var user = new IdentityUser
        {
            Username = command.Username.Trim().ToLowerInvariant(),
            Email = command.Email.Trim().ToLowerInvariant(),
            Phone = command.Phone?.Trim(),
            PasswordHash = _passwordHasher.HashPassword(command.Password),
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            IsActive = true,
            SecurityStamp = GenerateSecurityStamp(),
            PasswordChangedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        var id = await _userRepo.CreateAsync(user, ct);
        _logger.LogInformation("User created with ID {UserId}", id);
        return Result.Success(id);
    }

    public async Task<Result<IdentityUser>> GetUserAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null) return Result.Failure<IdentityUser>($"User {userId} not found.");
        return Result.Success(user);
    }

    public async Task<Result<IReadOnlyList<IdentityUser>>> GetUsersAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        var users = await _userRepo.GetAllAsync(page, pageSize, ct);
        return Result.Success(users);
    }

    public async Task<Result> UpdateUserAsync(UpdateUserCommand command, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(command.UserId, ct);
        if (user is null) return Result.Failure($"User {command.UserId} not found.");
        if (command.Phone is not null) user.Phone = command.Phone.Trim();
        if (command.FirstName is not null) user.FirstName = command.FirstName.Trim();
        if (command.LastName is not null) user.LastName = command.LastName.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, ct);
        return Result.Success();
    }

    public async Task<Result> EnableUserAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null) return Result.Failure($"User {userId} not found.");
        user.IsActive = true;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, ct);
        return Result.Success();
    }

    public async Task<Result> DisableUserAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null) return Result.Failure($"User {userId} not found.");
        user.IsActive = false;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, ct);
        return Result.Success();
    }

    private static string GenerateSecurityStamp() =>
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
}
