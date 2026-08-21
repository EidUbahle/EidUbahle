using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;

namespace CentralIdentity.UnitTests.Fakes;

/// <summary>In-memory fake used in place of the real ADO.NET-backed repository for testing.</summary>
public sealed class FakeUserApplicationRepository : IUserApplicationRepository
{
    private readonly Dictionary<long, IdentityUserApplication> _assignments = new();
    private long _nextId = 1;

    public void Add(IdentityUserApplication userApplication)
    {
        if (userApplication.UserApplicationId == 0)
            userApplication.UserApplicationId = _nextId++;

        _assignments[userApplication.UserApplicationId] = userApplication;
    }

    public Task<long> AssignAsync(IdentityUserApplication userApplication, CancellationToken ct = default)
    {
        userApplication.UserApplicationId = _nextId++;
        _assignments[userApplication.UserApplicationId] = userApplication;
        return Task.FromResult(userApplication.UserApplicationId);
    }

    public Task<IdentityUserApplication?> GetAsync(long userId, long applicationId, CancellationToken ct = default) =>
        Task.FromResult(_assignments.Values.FirstOrDefault(a => a.UserId == userId && a.ApplicationId == applicationId));

    public Task<IReadOnlyList<IdentityUserApplication>> GetUserApplicationsAsync(long userId, CancellationToken ct = default)
    {
        var results = _assignments.Values.Where(a => a.UserId == userId).ToList();
        return Task.FromResult<IReadOnlyList<IdentityUserApplication>>(results);
    }

    public Task<IReadOnlyList<IdentityUserApplication>> GetApplicationUsersAsync(long applicationId, CancellationToken ct = default)
    {
        var results = _assignments.Values.Where(a => a.ApplicationId == applicationId).ToList();
        return Task.FromResult<IReadOnlyList<IdentityUserApplication>>(results);
    }

    public Task<IReadOnlyList<IdentityUserApplication>> GetInactiveByThresholdAsync(DateTime threshold, int batchSize, CancellationToken ct = default)
    {
        var results = _assignments.Values
            .Where(ua => ua.IsActive
                && (ua.Status == "Active" || ua.Status is null)
                && (ua.LastActivityAtUtc.HasValue ? ua.LastActivityAtUtc.Value < threshold : ua.AssignedAtUtc < threshold))
            .Take(batchSize)
            .ToList();

        return Task.FromResult<IReadOnlyList<IdentityUserApplication>>(results);
    }

    public Task UpdateAsync(IdentityUserApplication userApplication, CancellationToken ct = default)
    {
        _assignments[userApplication.UserApplicationId] = userApplication;
        return Task.CompletedTask;
    }

    public Task UpdateActivityAsync(long userId, long applicationId, DateTime lastActivityAtUtc, CancellationToken ct = default)
    {
        var ua = _assignments.Values.FirstOrDefault(x => x.UserId == userId && x.ApplicationId == applicationId);
        if (ua is not null)
            ua.LastActivityAtUtc = lastActivityAtUtc;

        return Task.CompletedTask;
    }

    public Task RevokeForInactivityAsync(long userId, long applicationId, CancellationToken ct = default)
    {
        var ua = _assignments.Values.FirstOrDefault(x => x.UserId == userId && x.ApplicationId == applicationId && x.IsActive);
        if (ua is not null)
        {
            ua.IsActive = false;
            ua.RevokedAtUtc = DateTime.UtcNow;
            ua.RevocationReason = "InactivityRevocation";
            ua.Status = "Inactive";
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(long userId, long applicationId, CancellationToken ct = default) =>
        Task.FromResult(_assignments.Values.Any(a => a.UserId == userId && a.ApplicationId == applicationId));
}
