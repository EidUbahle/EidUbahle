using CentralIdentity.Domain.Entities;

namespace CentralIdentity.Application.Common.Interfaces;

public interface IUserApplicationRepository
{
    Task<long> AssignAsync(IdentityUserApplication userApplication, CancellationToken ct = default);
    Task<IdentityUserApplication?> GetAsync(long userId, long applicationId, CancellationToken ct = default);
    Task<IReadOnlyList<IdentityUserApplication>> GetUserApplicationsAsync(long userId, CancellationToken ct = default);
    Task<IReadOnlyList<IdentityUserApplication>> GetApplicationUsersAsync(long applicationId, CancellationToken ct = default);
    Task<IReadOnlyList<IdentityUserApplication>> GetInactiveByThresholdAsync(DateTime threshold, int batchSize, CancellationToken ct = default);
    Task UpdateAsync(IdentityUserApplication userApplication, CancellationToken ct = default);
    Task UpdateActivityAsync(long userId, long applicationId, DateTime lastActivityAtUtc, CancellationToken ct = default);
    Task RevokeForInactivityAsync(long userId, long applicationId, CancellationToken ct = default);
    Task<bool> ExistsAsync(long userId, long applicationId, CancellationToken ct = default);
}
