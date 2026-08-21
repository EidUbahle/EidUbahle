using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;
using CentralIdentity.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace CentralIdentity.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AuditLogRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task LogAsync(IdentityAuditLog log, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO [dbo].[IdentityAuditLogs]
                ([UserId],[ApplicationId],[EventType],[Severity],[IpAddress],[UserAgent],[Description],[CorrelationId],[CreatedAtUtc])
            VALUES
                (@UserId,@ApplicationId,@EventType,@Severity,@IpAddress,@UserAgent,@Description,@CorrelationId,@CreatedAtUtc);";

        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", (object?)log.UserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ApplicationId", (object?)log.ApplicationId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EventType", log.EventType);
        cmd.Parameters.AddWithValue("@Severity", log.Severity);
        cmd.Parameters.AddWithValue("@IpAddress", (object?)log.IpAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UserAgent", (object?)log.UserAgent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Description", log.Description);
        cmd.Parameters.AddWithValue("@CorrelationId", (object?)log.CorrelationId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAtUtc", log.CreatedAtUtc);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
