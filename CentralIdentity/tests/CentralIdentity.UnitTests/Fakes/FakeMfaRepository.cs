using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;

namespace CentralIdentity.UnitTests.Fakes;

public sealed class FakeMfaRepository : IMfaRepository
{
    private readonly List<IdentityMfaMethod> _methods = new();
    private readonly List<IdentityRecoveryCode> _codes = new();

    public Task<IdentityMfaMethod?> GetByUserAndTypeAsync(long userId, string methodType, CancellationToken ct)
        => Task.FromResult(_methods.FirstOrDefault(m => m.UserId == userId && m.MethodType == methodType));

    public Task<IReadOnlyList<IdentityMfaMethod>> GetByUserAsync(long userId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IdentityMfaMethod>>(_methods.Where(m => m.UserId == userId).ToList());

    public Task CreateOrUpdateAsync(IdentityMfaMethod method, CancellationToken ct)
    {
        var existing = _methods.FirstOrDefault(m => m.UserId == method.UserId && m.MethodType == method.MethodType);
        if (existing is not null)
        {
            _methods.Remove(existing);
        }

        _methods.Add(method);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<IdentityRecoveryCode>> GetActiveRecoveryCodesAsync(long userId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IdentityRecoveryCode>>(_codes.Where(c => c.UserId == userId && !c.IsUsed).ToList());

    public Task SaveRecoveryCodesAsync(long userId, IEnumerable<IdentityRecoveryCode> codes, CancellationToken ct)
    {
        _codes.RemoveAll(c => c.UserId == userId);
        _codes.AddRange(codes);
        return Task.CompletedTask;
    }

    public Task MarkRecoveryCodeUsedAsync(long recoveryCodeId, CancellationToken ct)
    {
        var code = _codes.FirstOrDefault(c => c.RecoveryCodeId == recoveryCodeId);
        if (code is not null)
        {
            code.IsUsed = true;
            code.UsedAtUtc = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }
}
