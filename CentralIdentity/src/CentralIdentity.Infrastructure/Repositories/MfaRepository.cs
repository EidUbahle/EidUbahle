using System.Data.Common;
using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;
using CentralIdentity.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace CentralIdentity.Infrastructure.Repositories;

public sealed class MfaRepository : IMfaRepository
{
    private readonly IDbConnectionFactory _db;

    public MfaRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IdentityMfaMethod?> GetByUserAndTypeAsync(long userId, string methodType, CancellationToken ct)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = "SELECT MfaMethodId,UserId,MethodType,SecretEncrypted,IsEnabled,IsVerified,CreatedAtUtc,EnabledAtUtc,DisabledAtUtc FROM IdentityMfaMethods WHERE UserId=@UserId AND MethodType=@Type";
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@UserId", userId));
        cmd.Parameters.Add(new SqlParameter("@Type", methodType));
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        return Map(reader);
    }

    public async Task<IReadOnlyList<IdentityMfaMethod>> GetByUserAsync(long userId, CancellationToken ct)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = "SELECT MfaMethodId,UserId,MethodType,SecretEncrypted,IsEnabled,IsVerified,CreatedAtUtc,EnabledAtUtc,DisabledAtUtc FROM IdentityMfaMethods WHERE UserId=@UserId";
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@UserId", userId));
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<IdentityMfaMethod>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(Map(reader));
        }

        return list;
    }

    public async Task CreateOrUpdateAsync(IdentityMfaMethod method, CancellationToken ct)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"MERGE IdentityMfaMethods AS target
            USING (SELECT @UserId AS UserId, @Type AS MethodType) AS source
            ON target.UserId = source.UserId AND target.MethodType = source.MethodType
            WHEN MATCHED THEN
                UPDATE SET SecretEncrypted=@Secret, IsEnabled=@IsEnabled, IsVerified=@IsVerified, EnabledAtUtc=@EnabledAt, DisabledAtUtc=@DisabledAt
            WHEN NOT MATCHED THEN
                INSERT (UserId,MethodType,SecretEncrypted,IsEnabled,IsVerified,CreatedAtUtc)
                VALUES (@UserId,@Type,@Secret,@IsEnabled,@IsVerified,@Now);";
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@UserId", method.UserId));
        cmd.Parameters.Add(new SqlParameter("@Type", method.MethodType));
        cmd.Parameters.Add(new SqlParameter("@Secret", method.SecretEncrypted));
        cmd.Parameters.Add(new SqlParameter("@IsEnabled", method.IsEnabled));
        cmd.Parameters.Add(new SqlParameter("@IsVerified", method.IsVerified));
        cmd.Parameters.Add(new SqlParameter("@EnabledAt", (object?)method.EnabledAtUtc ?? DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@DisabledAt", (object?)method.DisabledAtUtc ?? DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@Now", DateTime.UtcNow));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<IdentityRecoveryCode>> GetActiveRecoveryCodesAsync(long userId, CancellationToken ct)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = "SELECT RecoveryCodeId,UserId,CodeHash,IsUsed,UsedAtUtc,CreatedAtUtc FROM IdentityRecoveryCodes WHERE UserId=@UserId AND IsUsed=0";
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@UserId", userId));
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<IdentityRecoveryCode>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(new IdentityRecoveryCode
            {
                RecoveryCodeId = reader.GetInt64(0),
                UserId = reader.GetInt64(1),
                CodeHash = reader.GetString(2),
                IsUsed = reader.GetBoolean(3),
                UsedAtUtc = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                CreatedAtUtc = reader.GetDateTime(5)
            });
        }

        return list;
    }

    public async Task SaveRecoveryCodesAsync(long userId, IEnumerable<IdentityRecoveryCode> codes, CancellationToken ct)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        await using (var delCmd = conn.CreateCommand())
        {
            delCmd.CommandText = "DELETE FROM IdentityRecoveryCodes WHERE UserId=@UserId";
            delCmd.Parameters.Add(new SqlParameter("@UserId", userId));
            await delCmd.ExecuteNonQueryAsync(ct);
        }

        foreach (var code in codes)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO IdentityRecoveryCodes (UserId,CodeHash,IsUsed,CreatedAtUtc) VALUES (@UserId,@Hash,0,@Now)";
            cmd.Parameters.Add(new SqlParameter("@UserId", userId));
            cmd.Parameters.Add(new SqlParameter("@Hash", code.CodeHash));
            cmd.Parameters.Add(new SqlParameter("@Now", DateTime.UtcNow));
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    public async Task MarkRecoveryCodeUsedAsync(long recoveryCodeId, CancellationToken ct)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = "UPDATE IdentityRecoveryCodes SET IsUsed=1, UsedAtUtc=@Now WHERE RecoveryCodeId=@Id";
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@Now", DateTime.UtcNow));
        cmd.Parameters.Add(new SqlParameter("@Id", recoveryCodeId));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static IdentityMfaMethod Map(DbDataReader r) => new()
    {
        MfaMethodId = r.GetInt64(0),
        UserId = r.GetInt64(1),
        MethodType = r.GetString(2),
        SecretEncrypted = r.GetString(3),
        IsEnabled = r.GetBoolean(4),
        IsVerified = r.GetBoolean(5),
        CreatedAtUtc = r.GetDateTime(6),
        EnabledAtUtc = r.IsDBNull(7) ? null : r.GetDateTime(7),
        DisabledAtUtc = r.IsDBNull(8) ? null : r.GetDateTime(8)
    };
}
