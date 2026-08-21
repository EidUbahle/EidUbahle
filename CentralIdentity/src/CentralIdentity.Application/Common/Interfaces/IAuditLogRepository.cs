using CentralIdentity.Domain.Entities;

namespace CentralIdentity.Application.Common.Interfaces;

public interface IAuditLogRepository
{
    Task LogAsync(IdentityAuditLog log, CancellationToken ct);
}
