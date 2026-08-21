using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;
using CentralIdentity.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace CentralIdentity.Infrastructure.Repositories;

public sealed class UserApplicationRepository : IUserApplicationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<UserApplicationRepository> _logger;

    public UserApplicationRepository(IDbConnectionFactory connectionFactory, ILogger<UserApplicationRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<long> AssignAsync(IdentityUserApplication userApplication, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO [dbo].[IdentityUserApplications]
                ([UserId],[ApplicationId],[IsActive],[Status],[AssignedAtUtc],[SecurityStamp])
            VALUES
                (@UserId,@ApplicationId,@IsActive,@Status,@AssignedAtUtc,@SecurityStamp);
            SELECT SCOPE_IDENTITY();";

        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userApplication.UserId);
        cmd.Parameters.AddWithValue("@ApplicationId", userApplication.ApplicationId);
        cmd.Parameters.AddWithValue("@IsActive", userApplication.IsActive);
        cmd.Parameters.AddWithValue("@Status", (object?)userApplication.Status ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AssignedAtUtc", userApplication.AssignedAtUtc);
        cmd.Parameters.AddWithValue("@SecurityStamp", userApplication.SecurityStamp);
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result);
    }

    public async Task<IdentityUserApplication?> GetAsync(long userId, long applicationId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM [dbo].[IdentityUserApplications] WHERE [UserId] = @UserId AND [ApplicationId] = @ApplicationId";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<IdentityUserApplication>> GetUserApplicationsAsync(long userId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM [dbo].[IdentityUserApplications] WHERE [UserId] = @UserId ORDER BY [AssignedAtUtc] DESC";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var results = new List<IdentityUserApplication>();
        while (await reader.ReadAsync(ct))
            results.Add(Map(reader));
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<IdentityUserApplication>> GetApplicationUsersAsync(long applicationId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM [dbo].[IdentityUserApplications] WHERE [ApplicationId] = @ApplicationId ORDER BY [AssignedAtUtc] DESC";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var results = new List<IdentityUserApplication>();
        while (await reader.ReadAsync(ct))
            results.Add(Map(reader));
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<IdentityUserApplication>> GetInactiveByThresholdAsync(DateTime threshold, int batchSize, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT TOP (@BatchSize) *
            FROM [dbo].[IdentityUserApplications]
            WHERE [IsActive] = 1
              AND ([Status] = 'Active' OR [Status] IS NULL)
              AND (
                    ([LastActivityAtUtc] IS NOT NULL AND [LastActivityAtUtc] < @Threshold)
                 OR ([LastActivityAtUtc] IS NULL AND [AssignedAtUtc] < @Threshold)
              )
            ORDER BY COALESCE([LastActivityAtUtc], [AssignedAtUtc]) ASC";

        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@BatchSize", batchSize);
        cmd.Parameters.AddWithValue("@Threshold", threshold);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var results = new List<IdentityUserApplication>();
        while (await reader.ReadAsync(ct))
            results.Add(Map(reader));
        return results.AsReadOnly();
    }

    public async Task UpdateAsync(IdentityUserApplication userApplication, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE [dbo].[IdentityUserApplications]
            SET [IsActive]=@IsActive,[Status]=@Status,[LastAccessAtUtc]=@LastAccessAtUtc,[LastActivityAtUtc]=@LastActivityAtUtc,
                [RevokedAtUtc]=@RevokedAtUtc,[RevocationReason]=@RevocationReason,[SecurityStamp]=@SecurityStamp
            WHERE [UserApplicationId]=@UserApplicationId";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserApplicationId", userApplication.UserApplicationId);
        cmd.Parameters.AddWithValue("@IsActive", userApplication.IsActive);
        cmd.Parameters.AddWithValue("@Status", (object?)userApplication.Status ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LastAccessAtUtc", (object?)userApplication.LastAccessAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LastActivityAtUtc", (object?)userApplication.LastActivityAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RevokedAtUtc", (object?)userApplication.RevokedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RevocationReason", (object?)userApplication.RevocationReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SecurityStamp", userApplication.SecurityStamp);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateActivityAsync(long userId, long applicationId, DateTime lastActivityAtUtc, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE [dbo].[IdentityUserApplications]
            SET [LastActivityAtUtc] = @LastActivityAtUtc
            WHERE [UserId] = @UserId AND [ApplicationId] = @ApplicationId";

        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
        cmd.Parameters.AddWithValue("@LastActivityAtUtc", lastActivityAtUtc);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RevokeForInactivityAsync(long userId, long applicationId, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE [dbo].[IdentityUserApplications]
            SET [IsActive] = 0,
                [RevokedAtUtc] = SYSUTCDATETIME(),
                [RevocationReason] = 'InactivityRevocation',
                [Status] = 'Inactive'
            WHERE [UserId] = @UserId
              AND [ApplicationId] = @ApplicationId
              AND [IsActive] = 1";

        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> ExistsAsync(long userId, long applicationId, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(1) FROM [dbo].[IdentityUserApplications] WHERE [UserId] = @UserId AND [ApplicationId] = @ApplicationId";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
        var count = (int)(await cmd.ExecuteScalarAsync(ct))!;
        return count > 0;
    }

    private static IdentityUserApplication Map(SqlDataReader reader) => new()
    {
        UserApplicationId = reader.GetInt64(reader.GetOrdinal("UserApplicationId")),
        UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
        ApplicationId = reader.GetInt64(reader.GetOrdinal("ApplicationId")),
        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
        Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? null : reader.GetString(reader.GetOrdinal("Status")),
        AssignedAtUtc = reader.GetDateTime(reader.GetOrdinal("AssignedAtUtc")),
        LastAccessAtUtc = reader.IsDBNull(reader.GetOrdinal("LastAccessAtUtc")) ? null : reader.GetDateTime(reader.GetOrdinal("LastAccessAtUtc")),
        LastActivityAtUtc = reader.IsDBNull(reader.GetOrdinal("LastActivityAtUtc")) ? null : reader.GetDateTime(reader.GetOrdinal("LastActivityAtUtc")),
        RevokedAtUtc = reader.IsDBNull(reader.GetOrdinal("RevokedAtUtc")) ? null : reader.GetDateTime(reader.GetOrdinal("RevokedAtUtc")),
        RevocationReason = reader.IsDBNull(reader.GetOrdinal("RevocationReason")) ? null : reader.GetString(reader.GetOrdinal("RevocationReason")),
        SecurityStamp = reader.GetString(reader.GetOrdinal("SecurityStamp"))
    };
}
