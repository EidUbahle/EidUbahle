using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;
using CentralIdentity.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace CentralIdentity.Infrastructure.Repositories;

public sealed class PermissionRepository : IPermissionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PermissionRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<IdentityPermission?> GetByIdAsync(long permissionId, CancellationToken ct)
    {
        const string sql = "SELECT PermissionId,ApplicationId,PermissionCode,PermissionName,Description,IsActive,CreatedAtUtc,UpdatedAtUtc FROM IdentityPermissions WHERE PermissionId=@Id";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", permissionId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        return MapPermission(reader);
    }

    public async Task<IReadOnlyList<IdentityPermission>> GetByApplicationAsync(long applicationId, CancellationToken ct)
    {
        const string sql = "SELECT PermissionId,ApplicationId,PermissionCode,PermissionName,Description,IsActive,CreatedAtUtc,UpdatedAtUtc FROM IdentityPermissions WHERE ApplicationId=@AppId ORDER BY PermissionCode";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@AppId", applicationId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<IdentityPermission>();
        while (await reader.ReadAsync(ct)) list.Add(MapPermission(reader));
        return list;
    }

    public async Task<long> CreateAsync(IdentityPermission permission, CancellationToken ct)
    {
        const string sql = @"INSERT INTO IdentityPermissions (ApplicationId,PermissionCode,PermissionName,Description,IsActive,CreatedAtUtc)
            VALUES (@AppId,@Code,@Name,@Desc,1,@Now);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@AppId", permission.ApplicationId);
        cmd.Parameters.AddWithValue("@Code", permission.PermissionCode);
        cmd.Parameters.AddWithValue("@Name", permission.PermissionName);
        cmd.Parameters.AddWithValue("@Desc", (object?)permission.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result);
    }

    public async Task UpdateAsync(IdentityPermission permission, CancellationToken ct)
    {
        const string sql = "UPDATE IdentityPermissions SET PermissionName=@Name,Description=@Desc,IsActive=@IsActive,UpdatedAtUtc=@Now WHERE PermissionId=@Id";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Name", permission.PermissionName);
        cmd.Parameters.AddWithValue("@Desc", (object?)permission.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsActive", permission.IsActive);
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@Id", permission.PermissionId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    internal static IdentityPermission MapPermission(SqlDataReader reader) => new()
    {
        PermissionId = reader.GetInt64(0),
        ApplicationId = reader.GetInt64(1),
        PermissionCode = reader.GetString(2),
        PermissionName = reader.GetString(3),
        Description = reader.IsDBNull(4) ? null : reader.GetString(4),
        IsActive = reader.GetBoolean(5),
        CreatedAtUtc = reader.GetDateTime(6),
        UpdatedAtUtc = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
    };
}
