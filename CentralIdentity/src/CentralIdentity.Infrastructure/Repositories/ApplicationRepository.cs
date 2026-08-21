using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;
using CentralIdentity.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace CentralIdentity.Infrastructure.Repositories;

public sealed class ApplicationRepository : IApplicationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<ApplicationRepository> _logger;

    public ApplicationRepository(IDbConnectionFactory connectionFactory, ILogger<ApplicationRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<long> CreateAsync(IdentityApplication application, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO [dbo].[IdentityApplications]
                ([ApplicationCode],[ApplicationName],[Description],[ClientId],[ClientSecretHash],
                 [ClientType],[Audience],[AllowedRedirectUris],[AllowedOrigins],[IsActive],[CreatedAtUtc])
            VALUES
                (@ApplicationCode,@ApplicationName,@Description,@ClientId,@ClientSecretHash,
                 @ClientType,@Audience,@AllowedRedirectUris,@AllowedOrigins,@IsActive,@CreatedAtUtc);
            SELECT SCOPE_IDENTITY();";

        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ApplicationCode", application.ApplicationCode);
        cmd.Parameters.AddWithValue("@ApplicationName", application.ApplicationName);
        cmd.Parameters.AddWithValue("@Description", (object?)application.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ClientId", application.ClientId);
        cmd.Parameters.AddWithValue("@ClientSecretHash", (object?)application.ClientSecretHash ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ClientType", application.ClientType);
        cmd.Parameters.AddWithValue("@Audience", application.Audience);
        cmd.Parameters.AddWithValue("@AllowedRedirectUris", (object?)application.AllowedRedirectUris ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AllowedOrigins", (object?)application.AllowedOrigins ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsActive", application.IsActive);
        cmd.Parameters.AddWithValue("@CreatedAtUtc", application.CreatedAtUtc);
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result);
    }

    public async Task<IdentityApplication?> GetByIdAsync(long applicationId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM [dbo].[IdentityApplications] WHERE [ApplicationId] = @ApplicationId";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapApplication(reader) : null;
    }

    public async Task<IdentityApplication?> GetByClientIdAsync(string clientId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM [dbo].[IdentityApplications] WHERE [ClientId] = @ClientId";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ClientId", clientId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapApplication(reader) : null;
    }

    public async Task<IdentityApplication?> GetByApplicationCodeAsync(string applicationCode, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM [dbo].[IdentityApplications] WHERE [ApplicationCode] = @ApplicationCode";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ApplicationCode", applicationCode);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapApplication(reader) : null;
    }

    public async Task<IReadOnlyList<IdentityApplication>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM [dbo].[IdentityApplications]
            ORDER BY [CreatedAtUtc] DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var apps = new List<IdentityApplication>();
        while (await reader.ReadAsync(ct))
            apps.Add(MapApplication(reader));
        return apps.AsReadOnly();
    }

    public async Task UpdateAsync(IdentityApplication application, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE [dbo].[IdentityApplications]
            SET [ApplicationName]=@ApplicationName,[Description]=@Description,
                [AllowedRedirectUris]=@AllowedRedirectUris,[AllowedOrigins]=@AllowedOrigins,
                [IsActive]=@IsActive,[ClientSecretHash]=@ClientSecretHash,[UpdatedAtUtc]=@UpdatedAtUtc
            WHERE [ApplicationId]=@ApplicationId";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ApplicationId", application.ApplicationId);
        cmd.Parameters.AddWithValue("@ApplicationName", application.ApplicationName);
        cmd.Parameters.AddWithValue("@Description", (object?)application.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AllowedRedirectUris", (object?)application.AllowedRedirectUris ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AllowedOrigins", (object?)application.AllowedOrigins ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsActive", application.IsActive);
        cmd.Parameters.AddWithValue("@ClientSecretHash", (object?)application.ClientSecretHash ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UpdatedAtUtc", (object?)application.UpdatedAtUtc ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> ExistsByCodeAsync(string applicationCode, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(1) FROM [dbo].[IdentityApplications] WHERE [ApplicationCode] = @ApplicationCode";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ApplicationCode", applicationCode);
        var count = (int)(await cmd.ExecuteScalarAsync(ct))!;
        return count > 0;
    }

    public async Task<bool> ExistsByClientIdAsync(string clientId, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(1) FROM [dbo].[IdentityApplications] WHERE [ClientId] = @ClientId";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ClientId", clientId);
        var count = (int)(await cmd.ExecuteScalarAsync(ct))!;
        return count > 0;
    }

    private static IdentityApplication MapApplication(SqlDataReader reader) => new()
    {
        ApplicationId = reader.GetInt64(reader.GetOrdinal("ApplicationId")),
        ApplicationCode = reader.GetString(reader.GetOrdinal("ApplicationCode")),
        ApplicationName = reader.GetString(reader.GetOrdinal("ApplicationName")),
        Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
        ClientId = reader.GetString(reader.GetOrdinal("ClientId")),
        ClientSecretHash = reader.IsDBNull(reader.GetOrdinal("ClientSecretHash")) ? null : reader.GetString(reader.GetOrdinal("ClientSecretHash")),
        ClientType = reader.GetString(reader.GetOrdinal("ClientType")),
        Audience = reader.GetString(reader.GetOrdinal("Audience")),
        AllowedRedirectUris = reader.IsDBNull(reader.GetOrdinal("AllowedRedirectUris")) ? null : reader.GetString(reader.GetOrdinal("AllowedRedirectUris")),
        AllowedOrigins = reader.IsDBNull(reader.GetOrdinal("AllowedOrigins")) ? null : reader.GetString(reader.GetOrdinal("AllowedOrigins")),
        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
        CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
        UpdatedAtUtc = reader.IsDBNull(reader.GetOrdinal("UpdatedAtUtc")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAtUtc"))
    };
}
