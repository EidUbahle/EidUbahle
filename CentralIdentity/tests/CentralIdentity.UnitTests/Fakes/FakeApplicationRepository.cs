using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;

namespace CentralIdentity.UnitTests.Fakes;

/// <summary>In-memory fake used in place of the real ADO.NET-backed repository for testing.</summary>
public sealed class FakeApplicationRepository : IApplicationRepository
{
    private readonly Dictionary<long, IdentityApplication> _applications = new();
    private long _nextId = 1;

    public Task<long> CreateAsync(IdentityApplication application, CancellationToken ct = default)
    {
        application.ApplicationId = _nextId++;
        _applications[application.ApplicationId] = application;
        return Task.FromResult(application.ApplicationId);
    }

    public Task<IdentityApplication?> GetByIdAsync(long applicationId, CancellationToken ct = default) =>
        Task.FromResult(_applications.TryGetValue(applicationId, out var app) ? app : null);

    public Task<IdentityApplication?> GetByClientIdAsync(string clientId, CancellationToken ct = default) =>
        Task.FromResult(_applications.Values.FirstOrDefault(a => a.ClientId == clientId));

    public Task<IdentityApplication?> GetByApplicationCodeAsync(string applicationCode, CancellationToken ct = default) =>
        Task.FromResult(_applications.Values.FirstOrDefault(a =>
            string.Equals(a.ApplicationCode, applicationCode, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<IdentityApplication>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var results = _applications.Values
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Task.FromResult<IReadOnlyList<IdentityApplication>>(results);
    }

    public Task UpdateAsync(IdentityApplication application, CancellationToken ct = default)
    {
        _applications[application.ApplicationId] = application;
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByCodeAsync(string applicationCode, CancellationToken ct = default) =>
        Task.FromResult(_applications.Values.Any(a => string.Equals(a.ApplicationCode, applicationCode, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExistsByClientIdAsync(string clientId, CancellationToken ct = default) =>
        Task.FromResult(_applications.Values.Any(a => a.ClientId == clientId));
}
