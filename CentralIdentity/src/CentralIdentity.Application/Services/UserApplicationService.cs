using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Common;
using CentralIdentity.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CentralIdentity.Application.Services;

public interface IUserApplicationService
{
    Task<Result<long>> AssignUserToApplicationAsync(long userId, long applicationId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<IdentityUserApplication>>> GetUserApplicationsAsync(long userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<IdentityUserApplication>>> GetApplicationUsersAsync(long applicationId, CancellationToken ct = default);
    Task<Result> EnableUserApplicationAsync(long userId, long applicationId, CancellationToken ct = default);
    Task<Result> DisableUserApplicationAsync(long userId, long applicationId, CancellationToken ct = default);
    Task<Result> RevokeUserApplicationAsync(long userId, long applicationId, string? reason, CancellationToken ct = default);
}

public sealed class UserApplicationService : IUserApplicationService
{
    private readonly IUserApplicationRepository _repo;
    private readonly IUserRepository _userRepo;
    private readonly IApplicationRepository _appRepo;
    private readonly ILogger<UserApplicationService> _logger;

    public UserApplicationService(
        IUserApplicationRepository repo,
        IUserRepository userRepo,
        IApplicationRepository appRepo,
        ILogger<UserApplicationService> logger)
    {
        _repo = repo;
        _userRepo = userRepo;
        _appRepo = appRepo;
        _logger = logger;
    }

    public async Task<Result<long>> AssignUserToApplicationAsync(long userId, long applicationId, CancellationToken ct = default)
    {
        if (await _userRepo.GetByIdAsync(userId, ct) is null)
            return Result.Failure<long>($"User {userId} not found.");
        if (await _appRepo.GetByIdAsync(applicationId, ct) is null)
            return Result.Failure<long>($"Application {applicationId} not found.");
        if (await _repo.ExistsAsync(userId, applicationId, ct))
            return Result.Failure<long>("User is already assigned to this application.");

        var ua = new IdentityUserApplication
        {
            UserId = userId,
            ApplicationId = applicationId,
            IsActive = true,
            Status = "Active",
            AssignedAtUtc = DateTime.UtcNow,
            SecurityStamp = GenerateSecurityStamp()
        };
        var id = await _repo.AssignAsync(ua, ct);
        _logger.LogInformation("User {UserId} assigned to Application {ApplicationId}", userId, applicationId);
        return Result.Success(id);
    }

    public async Task<Result<IReadOnlyList<IdentityUserApplication>>> GetUserApplicationsAsync(long userId, CancellationToken ct = default)
        => Result.Success(await _repo.GetUserApplicationsAsync(userId, ct));

    public async Task<Result<IReadOnlyList<IdentityUserApplication>>> GetApplicationUsersAsync(long applicationId, CancellationToken ct = default)
        => Result.Success(await _repo.GetApplicationUsersAsync(applicationId, ct));

    public async Task<Result> EnableUserApplicationAsync(long userId, long applicationId, CancellationToken ct = default)
    {
        var ua = await _repo.GetAsync(userId, applicationId, ct);
        if (ua is null) return Result.Failure("UserApplication not found.");
        ua.IsActive = true;
        ua.Status = "Active";
        ua.RevokedAtUtc = null;
        ua.RevocationReason = null;
        await _repo.UpdateAsync(ua, ct);
        return Result.Success();
    }

    public async Task<Result> DisableUserApplicationAsync(long userId, long applicationId, CancellationToken ct = default)
    {
        var ua = await _repo.GetAsync(userId, applicationId, ct);
        if (ua is null) return Result.Failure("UserApplication not found.");
        ua.IsActive = false;
        if (!string.Equals(ua.Status, "Revoked", StringComparison.Ordinal))
            ua.Status = "Inactive";
        await _repo.UpdateAsync(ua, ct);
        return Result.Success();
    }

    public async Task<Result> RevokeUserApplicationAsync(long userId, long applicationId, string? reason, CancellationToken ct = default)
    {
        var ua = await _repo.GetAsync(userId, applicationId, ct);
        if (ua is null) return Result.Failure("UserApplication not found.");
        ua.IsActive = false;
        ua.Status = "Revoked";
        ua.RevokedAtUtc = DateTime.UtcNow;
        ua.RevocationReason = reason;
        ua.SecurityStamp = GenerateSecurityStamp();
        await _repo.UpdateAsync(ua, ct);
        _logger.LogInformation(
            "UserApplication (User={UserId}, App={AppId}) revoked. Reason: {Reason}",
            userId,
            applicationId,
            SanitizeForLog(reason));
        return Result.Success();
    }

    private static string GenerateSecurityStamp() =>
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

    private static string? SanitizeForLog(string? value) =>
        value?
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
}
