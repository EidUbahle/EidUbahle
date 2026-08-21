using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;
using CentralIdentity.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace CentralIdentity.Infrastructure.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<RoleRepository> _logger;

    public RoleRepository(IDbConnectionFactory connectionFactory, ILogger<RoleRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<IdentityRole?> GetByIdAsync(long roleId, CancellationToken ct)
    {
        const string sql = "SELECT RoleId,ApplicationId,RoleCode,RoleName,Description,IsActive,CreatedAtUtc,UpdatedAtUtc FROM IdentityRoles WHERE RoleId=@RoleId";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@RoleId", roleId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        return MapRole(reader);
    }

    public async Task<IReadOnlyList<IdentityRole>> GetByApplicationAsync(long applicationId, CancellationToken ct)
    {
        const string sql = "SELECT RoleId,ApplicationId,RoleCode,RoleName,Description,IsActive,CreatedAtUtc,UpdatedAtUtc FROM IdentityRoles WHERE ApplicationId=@ApplicationId ORDER BY RoleCode";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<IdentityRole>();
        while (await reader.ReadAsync(ct)) list.Add(MapRole(reader));
        return list;
    }

    public async Task<long> CreateAsync(IdentityRole role, CancellationToken ct)
    {
        const string sql = @"INSERT INTO IdentityRoles (ApplicationId,RoleCode,RoleName,Description,IsActive,CreatedAtUtc)
            VALUES (@AppId,@RoleCode,@RoleName,@Desc,1,@Now);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@AppId", role.ApplicationId);
        cmd.Parameters.AddWithValue("@RoleCode", role.RoleCode);
        cmd.Parameters.AddWithValue("@RoleName", role.RoleName);
        cmd.Parameters.AddWithValue("@Desc", (object?)role.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result);
    }

    public async Task UpdateAsync(IdentityRole role, CancellationToken ct)
    {
        const string sql = "UPDATE IdentityRoles SET RoleName=@RoleName,Description=@Desc,IsActive=@IsActive,UpdatedAtUtc=@Now WHERE RoleId=@RoleId";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@RoleName", role.RoleName);
        cmd.Parameters.AddWithValue("@Desc", (object?)role.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsActive", role.IsActive);
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@RoleId", role.RoleId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<IdentityPermission>> GetPermissionsAsync(long roleId, CancellationToken ct)
    {
        const string sql = @"SELECT p.PermissionId,p.ApplicationId,p.PermissionCode,p.PermissionName,p.Description,p.IsActive,p.CreatedAtUtc,p.UpdatedAtUtc
            FROM IdentityPermissions p
            INNER JOIN IdentityRolePermissions rp ON rp.PermissionId=p.PermissionId
            WHERE rp.RoleId=@RoleId AND p.IsActive=1";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@RoleId", roleId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<IdentityPermission>();
        while (await reader.ReadAsync(ct)) list.Add(PermissionRepository.MapPermission(reader));
        return list;
    }

    public async Task AssignPermissionAsync(long roleId, long permissionId, CancellationToken ct)
    {
        const string sql = @"IF NOT EXISTS (SELECT 1 FROM IdentityRolePermissions WHERE RoleId=@RoleId AND PermissionId=@PermId)
            INSERT INTO IdentityRolePermissions (RoleId,PermissionId,AssignedAtUtc) VALUES (@RoleId,@PermId,@Now)";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@RoleId", roleId);
        cmd.Parameters.AddWithValue("@PermId", permissionId);
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RemovePermissionAsync(long roleId, long permissionId, CancellationToken ct)
    {
        const string sql = "DELETE FROM IdentityRolePermissions WHERE RoleId=@RoleId AND PermissionId=@PermId";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@RoleId", roleId);
        cmd.Parameters.AddWithValue("@PermId", permissionId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static IdentityRole MapRole(SqlDataReader reader) => new()
    {
        RoleId = reader.GetInt64(0),
        ApplicationId = reader.GetInt64(1),
        RoleCode = reader.GetString(2),
        RoleName = reader.GetString(3),
        Description = reader.IsDBNull(4) ? null : reader.GetString(4),
        IsActive = reader.GetBoolean(5),
        CreatedAtUtc = reader.GetDateTime(6),
        UpdatedAtUtc = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
    };
}
