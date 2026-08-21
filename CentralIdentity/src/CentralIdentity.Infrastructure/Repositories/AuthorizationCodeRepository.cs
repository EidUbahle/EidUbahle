using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;
using CentralIdentity.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace CentralIdentity.Infrastructure.Repositories;

public sealed class AuthorizationCodeRepository : IAuthorizationCodeRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<AuthorizationCodeRepository> _logger;

    public AuthorizationCodeRepository(IDbConnectionFactory connectionFactory, ILogger<AuthorizationCodeRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task StoreAsync(AuthorizationCode code, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO [dbo].[IdentityAuthorizationCodes]
                ([CodeHash],[UserId],[ApplicationId],[RedirectUri],[ClientId],[Scope],
                 [CodeChallenge],[CodeChallengeMethod],[IsUsed],[CreatedAtUtc],[ExpiresAtUtc])
            VALUES
                (@CodeHash,@UserId,@ApplicationId,@RedirectUri,@ClientId,@Scope,
                 @CodeChallenge,@CodeChallengeMethod,@IsUsed,@CreatedAtUtc,@ExpiresAtUtc);";

        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@CodeHash", code.CodeHash);
        cmd.Parameters.AddWithValue("@UserId", code.UserId);
        cmd.Parameters.AddWithValue("@ApplicationId", code.ApplicationId);
        cmd.Parameters.AddWithValue("@RedirectUri", code.RedirectUri);
        cmd.Parameters.AddWithValue("@ClientId", code.ClientId);
        cmd.Parameters.AddWithValue("@Scope", code.Scope);
        cmd.Parameters.AddWithValue("@CodeChallenge", (object?)code.CodeChallenge ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CodeChallengeMethod", (object?)code.CodeChallengeMethod ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsUsed", code.IsUsed);
        cmd.Parameters.AddWithValue("@CreatedAtUtc", code.CreatedAtUtc);
        cmd.Parameters.AddWithValue("@ExpiresAtUtc", code.ExpiresAtUtc);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<AuthorizationCode?> GetByHashAsync(string codeHash, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM [dbo].[IdentityAuthorizationCodes] WHERE [CodeHash] = @CodeHash";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@CodeHash", codeHash);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task MarkAsUsedAsync(string codeHash, CancellationToken ct = default)
    {
        const string sql = "UPDATE [dbo].[IdentityAuthorizationCodes] SET [IsUsed] = 1 WHERE [CodeHash] = @CodeHash";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@CodeHash", codeHash);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteExpiredAsync(CancellationToken ct = default)
    {
        const string sql = "DELETE FROM [dbo].[IdentityAuthorizationCodes] WHERE [ExpiresAtUtc] <= @Now";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
        var deleted = await cmd.ExecuteNonQueryAsync(ct);
        _logger.LogInformation("Deleted {Count} expired authorization codes", deleted);
    }

    private static AuthorizationCode Map(SqlDataReader reader) => new()
    {
        CodeId = reader.GetInt64(reader.GetOrdinal("CodeId")),
        CodeHash = reader.GetString(reader.GetOrdinal("CodeHash")),
        UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
        ApplicationId = reader.GetInt64(reader.GetOrdinal("ApplicationId")),
        RedirectUri = reader.GetString(reader.GetOrdinal("RedirectUri")),
        ClientId = reader.GetString(reader.GetOrdinal("ClientId")),
        Scope = reader.GetString(reader.GetOrdinal("Scope")),
        CodeChallenge = reader.IsDBNull(reader.GetOrdinal("CodeChallenge")) ? null : reader.GetString(reader.GetOrdinal("CodeChallenge")),
        CodeChallengeMethod = reader.IsDBNull(reader.GetOrdinal("CodeChallengeMethod")) ? null : reader.GetString(reader.GetOrdinal("CodeChallengeMethod")),
        IsUsed = reader.GetBoolean(reader.GetOrdinal("IsUsed")),
        CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
        ExpiresAtUtc = reader.GetDateTime(reader.GetOrdinal("ExpiresAtUtc"))
    };
}
