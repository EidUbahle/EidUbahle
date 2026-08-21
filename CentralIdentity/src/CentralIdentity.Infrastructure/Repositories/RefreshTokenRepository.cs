using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;
using CentralIdentity.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace CentralIdentity.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RefreshTokenRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IdentityRefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct)
    {
        const string sql = "SELECT * FROM [dbo].[IdentityRefreshTokens] WHERE [TokenHash] = @TokenHash";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@TokenHash", tokenHash);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task CreateAsync(IdentityRefreshToken token, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO [dbo].[IdentityRefreshTokens]
                ([RefreshTokenId],[UserId],[ApplicationId],[SessionId],[TokenHash],[CreatedAtUtc],[ExpiresAtUtc],
                 [LastUsedAtUtc],[RevokedAtUtc],[ReplacedByTokenId],[RevocationReason],[TokenFamilyId],
                 [CreatedIpAddress],[LastUsedIpAddress],[UserAgent],[Scope])
            VALUES
                (@RefreshTokenId,@UserId,@ApplicationId,@SessionId,@TokenHash,@CreatedAtUtc,@ExpiresAtUtc,
                 @LastUsedAtUtc,@RevokedAtUtc,@ReplacedByTokenId,@RevocationReason,@TokenFamilyId,
                 @CreatedIpAddress,@LastUsedIpAddress,@UserAgent,@Scope);";

        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@RefreshTokenId", token.RefreshTokenId);
        cmd.Parameters.AddWithValue("@UserId", token.UserId);
        cmd.Parameters.AddWithValue("@ApplicationId", token.ApplicationId);
        cmd.Parameters.AddWithValue("@SessionId", token.SessionId);
        cmd.Parameters.AddWithValue("@TokenHash", token.TokenHash);
        cmd.Parameters.AddWithValue("@CreatedAtUtc", token.CreatedAtUtc);
        cmd.Parameters.AddWithValue("@ExpiresAtUtc", token.ExpiresAtUtc);
        cmd.Parameters.AddWithValue("@LastUsedAtUtc", (object?)token.LastUsedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RevokedAtUtc", (object?)token.RevokedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ReplacedByTokenId", (object?)token.ReplacedByTokenId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RevocationReason", (object?)token.RevocationReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TokenFamilyId", token.TokenFamilyId);
        cmd.Parameters.AddWithValue("@CreatedIpAddress", (object?)token.CreatedIpAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LastUsedIpAddress", (object?)token.LastUsedIpAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UserAgent", (object?)token.UserAgent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Scope", token.Scope);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public Task RevokeAsync(Guid refreshTokenId, string reason, CancellationToken ct) =>
        ExecuteRevokeAsync(
            "UPDATE [dbo].[IdentityRefreshTokens] SET [RevokedAtUtc] = @Now, [RevocationReason] = @Reason WHERE [RefreshTokenId] = @Id AND [RevokedAtUtc] IS NULL",
            ("@Id", refreshTokenId), reason, ct);

    public Task RevokeByFamilyAsync(Guid familyId, string reason, CancellationToken ct) =>
        ExecuteRevokeAsync(
            "UPDATE [dbo].[IdentityRefreshTokens] SET [RevokedAtUtc] = @Now, [RevocationReason] = @Reason WHERE [TokenFamilyId] = @Id AND [RevokedAtUtc] IS NULL",
            ("@Id", familyId), reason, ct);

    public Task RevokeBySessionAsync(Guid sessionId, string reason, CancellationToken ct) =>
        ExecuteRevokeAsync(
            "UPDATE [dbo].[IdentityRefreshTokens] SET [RevokedAtUtc] = @Now, [RevocationReason] = @Reason WHERE [SessionId] = @Id AND [RevokedAtUtc] IS NULL",
            ("@Id", sessionId), reason, ct);

    public Task RevokeByUserApplicationAsync(long userId, long applicationId, string reason, CancellationToken ct) =>
        ExecuteRevokeByUserAndMaybeAppAsync(userId, applicationId, includeApplication: true, reason, ct);

    public Task RevokeAllByUserAsync(long userId, string reason, CancellationToken ct) =>
        ExecuteRevokeByUserAndMaybeAppAsync(userId, applicationId: 0, includeApplication: false, reason, ct);

    private async Task ExecuteRevokeAsync(string sql, (string Name, object Value) idParameter, string reason, CancellationToken ct)
    {
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(idParameter.Name, idParameter.Value);
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@Reason", reason);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task ExecuteRevokeByUserAndMaybeAppAsync(long userId, long applicationId, bool includeApplication, string reason, CancellationToken ct)
    {
        var sql = includeApplication
            ? "UPDATE [dbo].[IdentityRefreshTokens] SET [RevokedAtUtc] = @Now, [RevocationReason] = @Reason WHERE [UserId] = @UserId AND [ApplicationId] = @ApplicationId AND [RevokedAtUtc] IS NULL"
            : "UPDATE [dbo].[IdentityRefreshTokens] SET [RevokedAtUtc] = @Now, [RevocationReason] = @Reason WHERE [UserId] = @UserId AND [RevokedAtUtc] IS NULL";

        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        if (includeApplication)
            cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@Reason", reason);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static IdentityRefreshToken Map(SqlDataReader reader) => new()
    {
        RefreshTokenId = reader.GetGuid(reader.GetOrdinal("RefreshTokenId")),
        UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
        ApplicationId = reader.GetInt64(reader.GetOrdinal("ApplicationId")),
        SessionId = reader.GetGuid(reader.GetOrdinal("SessionId")),
        TokenHash = reader.GetString(reader.GetOrdinal("TokenHash")),
        CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
        ExpiresAtUtc = reader.GetDateTime(reader.GetOrdinal("ExpiresAtUtc")),
        LastUsedAtUtc = reader.IsDBNull(reader.GetOrdinal("LastUsedAtUtc")) ? null : reader.GetDateTime(reader.GetOrdinal("LastUsedAtUtc")),
        RevokedAtUtc = reader.IsDBNull(reader.GetOrdinal("RevokedAtUtc")) ? null : reader.GetDateTime(reader.GetOrdinal("RevokedAtUtc")),
        ReplacedByTokenId = reader.IsDBNull(reader.GetOrdinal("ReplacedByTokenId")) ? null : reader.GetGuid(reader.GetOrdinal("ReplacedByTokenId")),
        RevocationReason = reader.IsDBNull(reader.GetOrdinal("RevocationReason")) ? null : reader.GetString(reader.GetOrdinal("RevocationReason")),
        TokenFamilyId = reader.GetGuid(reader.GetOrdinal("TokenFamilyId")),
        CreatedIpAddress = reader.IsDBNull(reader.GetOrdinal("CreatedIpAddress")) ? null : reader.GetString(reader.GetOrdinal("CreatedIpAddress")),
        LastUsedIpAddress = reader.IsDBNull(reader.GetOrdinal("LastUsedIpAddress")) ? null : reader.GetString(reader.GetOrdinal("LastUsedIpAddress")),
        UserAgent = reader.IsDBNull(reader.GetOrdinal("UserAgent")) ? null : reader.GetString(reader.GetOrdinal("UserAgent")),
        Scope = reader.IsDBNull(reader.GetOrdinal("Scope")) ? string.Empty : reader.GetString(reader.GetOrdinal("Scope"))
    };
}
