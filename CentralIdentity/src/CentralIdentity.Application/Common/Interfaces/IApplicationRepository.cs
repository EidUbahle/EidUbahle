using CentralIdentity.Domain.Entities;

namespace CentralIdentity.Application.Common.Interfaces;

public interface IApplicationRepository
{
    Task<long> CreateAsync(IdentityApplication application, CancellationToken ct = default);
    Task<IdentityApplication?> GetByIdAsync(long applicationId, CancellationToken ct = default);
    Task<IdentityApplication?> GetByClientIdAsync(string clientId, CancellationToken ct = default);
    Task<IdentityApplication?> GetByApplicationCodeAsync(string applicationCode, CancellationToken ct = default);
    Task<IReadOnlyList<IdentityApplication>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task UpdateAsync(IdentityApplication application, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string applicationCode, CancellationToken ct = default);
    Task<bool> ExistsByClientIdAsync(string clientId, CancellationToken ct = default);
}
