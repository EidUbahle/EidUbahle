using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;

namespace CentralIdentity.IntegrationTests.Fakes;

public sealed class FakeAuditLogRepository : IAuditLogRepository
{
    private readonly List<IdentityAuditLog> _logs = new();

    public IReadOnlyList<IdentityAuditLog> Logs => _logs.AsReadOnly();

    public Task LogAsync(IdentityAuditLog log, CancellationToken ct)
    {
        _logs.Add(log);
        return Task.CompletedTask;
    }
}
