using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace CentralIdentity.Infrastructure.Data;

public sealed class SqlServerHealthCheck : IHealthCheck
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<SqlServerHealthCheck> _logger;

    public SqlServerHealthCheck(IDbConnectionFactory connectionFactory, ILogger<SqlServerHealthCheck> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy("SQL Server connection is healthy.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL Server health check failed.");
            return HealthCheckResult.Unhealthy("SQL Server connection failed.", ex);
        }
    }
}
