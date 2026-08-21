using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;
using CentralIdentity.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace CentralIdentity.Infrastructure.Repositories;

public sealed class SessionRepository : ISessionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SessionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IdentitySession?> GetByIdAsync(Guid sessionId, CancellationToken ct)
    {
        const string sql = "SELECT * FROM [dbo].[IdentitySessions] WHERE [SessionId] = @SessionId";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@SessionId", sessionId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public Task<IReadOnlyList<IdentitySession>> GetActiveByUserAsync(long userId, CancellationToken ct) =>
        QueryActiveAsync("SELECT * FROM [dbo].[IdentitySessions] WHERE [UserId] = @UserId AND [IsActive] = 1 AND [RevokedAtUtc] IS NULL AND [ExpiresAtUtc] > @Now ORDER BY [LastActivityAtUtc] DESC",
            cmd => cmd.Parameters.AddWithValue("@UserId", userId), ct);

    public Task<IReadOnlyList<IdentitySession>> GetActiveByUserApplicationAsync(long userId, long applicationId, CancellationToken ct) =>
        QueryActiveAsync("SELECT * FROM [dbo].[IdentitySessions] WHERE [UserId] = @UserId AND [ApplicationId] = @ApplicationId AND [IsActive] = 1 AND [RevokedAtUtc] IS NULL AND [ExpiresAtUtc] > @Now ORDER BY [LastActivityAtUtc] DESC",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
            }, ct);

    public async Task CreateAsync(IdentitySession session, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO [dbo].[IdentitySessions]
                ([SessionId],[UserId],[ApplicationId],[ClientId],[CreatedAtUtc],[LastActivityAtUtc],[ExpiresAtUtc],
                 [RevokedAtUtc],[RevocationReason],[IpAddress],[UserAgent],[DeviceId],[SecurityStamp],[IsActive])
            VALUES
                (@SessionId,@UserId,@ApplicationId,@ClientId,@CreatedAtUtc,@LastActivityAtUtc,@ExpiresAtUtc,
                 @RevokedAtUtc,@RevocationReason,@IpAddress,@UserAgent,@DeviceId,@SecurityStamp,@IsActive);";

        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@SessionId", session.SessionId);
        cmd.Parameters.AddWithValue("@UserId", session.UserId);
        cmd.Parameters.AddWithValue("@ApplicationId", session.ApplicationId);
        cmd.Parameters.AddWithValue("@ClientId", session.ClientId);
        cmd.Parameters.AddWithValue("@CreatedAtUtc", session.CreatedAtUtc);
        cmd.Parameters.AddWithValue("@LastActivityAtUtc", session.LastActivityAtUtc);
        cmd.Parameters.AddWithValue("@ExpiresAtUtc", session.ExpiresAtUtc);
        cmd.Parameters.AddWithValue("@RevokedAtUtc", (object?)session.RevokedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RevocationReason", (object?)session.RevocationReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IpAddress", (object?)session.IpAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UserAgent", (object?)session.UserAgent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DeviceId", (object?)session.DeviceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SecurityStamp", session.SecurityStamp);
        cmd.Parameters.AddWithValue("@IsActive", session.IsActive);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateActivityAsync(Guid sessionId, DateTime lastActivityAtUtc, CancellationToken ct)
    {
        const string sql = "UPDATE [dbo].[IdentitySessions] SET [LastActivityAtUtc] = @LastActivityAtUtc WHERE [SessionId] = @SessionId";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@SessionId", sessionId);
        cmd.Parameters.AddWithValue("@LastActivityAtUtc", lastActivityAtUtc);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public Task RevokeAsync(Guid sessionId, string reason, CancellationToken ct) =>
        ExecuteRevokeAsync(
            "UPDATE [dbo].[IdentitySessions] SET [IsActive] = 0, [RevokedAtUtc] = @Now, [RevocationReason] = @Reason WHERE [SessionId] = @SessionId AND [RevokedAtUtc] IS NULL",
            cmd => cmd.Parameters.AddWithValue("@SessionId", sessionId), ct, reason);

    public Task RevokeByUserApplicationAsync(long userId, long applicationId, string reason, CancellationToken ct) =>
        ExecuteRevokeAsync(
            "UPDATE [dbo].[IdentitySessions] SET [IsActive] = 0, [RevokedAtUtc] = @Now, [RevocationReason] = @Reason WHERE [UserId] = @UserId AND [ApplicationId] = @ApplicationId AND [RevokedAtUtc] IS NULL",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
            }, ct, reason);

    public Task RevokeAllByUserAsync(long userId, string reason, CancellationToken ct) =>
        ExecuteRevokeAsync(
            "UPDATE [dbo].[IdentitySessions] SET [IsActive] = 0, [RevokedAtUtc] = @Now, [RevocationReason] = @Reason WHERE [UserId] = @UserId AND [RevokedAtUtc] IS NULL",
            cmd => cmd.Parameters.AddWithValue("@UserId", userId), ct, reason);

    private async Task<IReadOnlyList<IdentitySession>> QueryActiveAsync(string sql, Action<SqlCommand> configure, CancellationToken ct)
    {
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        configure(cmd);
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var results = new List<IdentitySession>();
        while (await reader.ReadAsync(ct))
            results.Add(Map(reader));
        return results;
    }

    private async Task ExecuteRevokeAsync(string sql, Action<SqlCommand> configure, CancellationToken ct, string reason)
    {
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        configure(cmd);
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@Reason", reason);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static IdentitySession Map(SqlDataReader reader) => new()
    {
        SessionId = reader.GetGuid(reader.GetOrdinal("SessionId")),
        UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
        ApplicationId = reader.GetInt64(reader.GetOrdinal("ApplicationId")),
        ClientId = reader.GetString(reader.GetOrdinal("ClientId")),
        CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
        LastActivityAtUtc = reader.GetDateTime(reader.GetOrdinal("LastActivityAtUtc")),
        ExpiresAtUtc = reader.GetDateTime(reader.GetOrdinal("ExpiresAtUtc")),
        RevokedAtUtc = reader.IsDBNull(reader.GetOrdinal("RevokedAtUtc")) ? null : reader.GetDateTime(reader.GetOrdinal("RevokedAtUtc")),
        RevocationReason = reader.IsDBNull(reader.GetOrdinal("RevocationReason")) ? null : reader.GetString(reader.GetOrdinal("RevocationReason")),
        IpAddress = reader.IsDBNull(reader.GetOrdinal("IpAddress")) ? null : reader.GetString(reader.GetOrdinal("IpAddress")),
        UserAgent = reader.IsDBNull(reader.GetOrdinal("UserAgent")) ? null : reader.GetString(reader.GetOrdinal("UserAgent")),
        DeviceId = reader.IsDBNull(reader.GetOrdinal("DeviceId")) ? null : reader.GetString(reader.GetOrdinal("DeviceId")),
        SecurityStamp = reader.GetString(reader.GetOrdinal("SecurityStamp")),
        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
    };
}
