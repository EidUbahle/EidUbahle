using CentralIdentity.Domain.Entities;

namespace CentralIdentity.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<long> CreateAsync(IdentityUser user, CancellationToken ct = default);
    Task<IdentityUser?> GetByIdAsync(long userId, CancellationToken ct = default);
    Task<IdentityUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IdentityUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<IReadOnlyList<IdentityUser>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task UpdateAsync(IdentityUser user, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default);
}
