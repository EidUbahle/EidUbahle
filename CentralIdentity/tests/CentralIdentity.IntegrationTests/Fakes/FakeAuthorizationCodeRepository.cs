using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;

namespace CentralIdentity.IntegrationTests.Fakes;

/// <summary>In-memory fake used in place of the real ADO.NET-backed repository for testing.</summary>
public sealed class FakeAuthorizationCodeRepository : IAuthorizationCodeRepository
{
    private readonly Dictionary<string, AuthorizationCode> _codes = new();
    private long _nextId = 1;

    public Task StoreAsync(AuthorizationCode code, CancellationToken ct = default)
    {
        code.CodeId = _nextId++;
        _codes[code.CodeHash] = code;
        return Task.CompletedTask;
    }

    public Task<AuthorizationCode?> GetByHashAsync(string codeHash, CancellationToken ct = default) =>
        Task.FromResult(_codes.TryGetValue(codeHash, out var code) ? code : null);

    public Task MarkAsUsedAsync(string codeHash, CancellationToken ct = default)
    {
        if (_codes.TryGetValue(codeHash, out var code))
        {
            code.IsUsed = true;
        }
        return Task.CompletedTask;
    }

    public Task DeleteExpiredAsync(CancellationToken ct = default)
    {
        var expired = _codes.Where(kv => kv.Value.ExpiresAtUtc < DateTime.UtcNow).Select(kv => kv.Key).ToList();
        foreach (var key in expired)
        {
            _codes.Remove(key);
        }
        return Task.CompletedTask;
    }
}
