using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;

namespace CentralIdentity.IntegrationTests.Fakes;

/// <summary>In-memory fake used in place of the real ADO.NET-backed repository for testing.</summary>
public sealed class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<long, IdentityUser> _users = new();
    private long _nextId = 1;

    public Task<long> CreateAsync(IdentityUser user, CancellationToken ct = default)
    {
        user.UserId = _nextId++;
        _users[user.UserId] = user;
        return Task.FromResult(user.UserId);
    }

    public Task<IdentityUser?> GetByIdAsync(long userId, CancellationToken ct = default) =>
        Task.FromResult(_users.TryGetValue(userId, out var user) ? user : null);

    public Task<IdentityUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(_users.Values.FirstOrDefault(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<IdentityUser?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        Task.FromResult(_users.Values.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<IdentityUser>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var results = _users.Values
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Task.FromResult<IReadOnlyList<IdentityUser>>(results);
    }

    public Task UpdateAsync(IdentityUser user, CancellationToken ct = default)
    {
        _users[user.UserId] = user;
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(_users.Values.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default) =>
        Task.FromResult(_users.Values.Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)));
}
