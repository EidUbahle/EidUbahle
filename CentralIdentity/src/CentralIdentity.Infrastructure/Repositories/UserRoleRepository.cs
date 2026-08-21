using CentralIdentity.Application.Common.Interfaces;
using CentralIdentity.Domain.Entities;
using CentralIdentity.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace CentralIdentity.Infrastructure.Repositories;

public sealed class UserRoleRepository : IUserRoleRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRoleRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<IReadOnlyList<IdentityUserRole>> GetActiveByUserApplicationAsync(long userId, long applicationId, CancellationToken ct)
    {
        const string sql = "SELECT UserRoleId,UserId,ApplicationId,RoleId,AssignedAtUtc,RevokedAtUtc,IsActive FROM IdentityUserRoles WHERE UserId=@UserId AND ApplicationId=@AppId AND IsActive=1";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@AppId", applicationId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<IdentityUserRole>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(new IdentityUserRole
            {
                UserRoleId = reader.GetInt64(0),
                UserId = reader.GetInt64(1),
                ApplicationId = reader.GetInt64(2),
                RoleId = reader.GetInt64(3),
                AssignedAtUtc = reader.GetDateTime(4),
                RevokedAtUtc = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                IsActive = reader.GetBoolean(6)
            });
        }

        return list;
    }

    public async Task AssignAsync(IdentityUserRole userRole, CancellationToken ct)
    {
        const string sql = @"IF NOT EXISTS (SELECT 1 FROM IdentityUserRoles WHERE UserId=@UserId AND ApplicationId=@AppId AND RoleId=@RoleId AND IsActive=1)
            INSERT INTO IdentityUserRoles (UserId,ApplicationId,RoleId,AssignedAtUtc,IsActive) VALUES (@UserId,@AppId,@RoleId,@Now,1)";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userRole.UserId);
        cmd.Parameters.AddWithValue("@AppId", userRole.ApplicationId);
        cmd.Parameters.AddWithValue("@RoleId", userRole.RoleId);
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RevokeAsync(long userId, long applicationId, long roleId, CancellationToken ct)
    {
        const string sql = "UPDATE IdentityUserRoles SET IsActive=0,RevokedAtUtc=@Now WHERE UserId=@UserId AND ApplicationId=@AppId AND RoleId=@RoleId AND IsActive=1";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@AppId", applicationId);
        cmd.Parameters.AddWithValue("@RoleId", roleId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<IdentityPermission>> GetEffectivePermissionsAsync(long userId, long applicationId, CancellationToken ct)
    {
        const string sql = @"SELECT DISTINCT p.PermissionId,p.ApplicationId,p.PermissionCode,p.PermissionName,p.Description,p.IsActive,p.CreatedAtUtc,p.UpdatedAtUtc
            FROM IdentityPermissions p
            INNER JOIN IdentityRolePermissions rp ON rp.PermissionId=p.PermissionId
            INNER JOIN IdentityRoles r ON r.RoleId=rp.RoleId
            INNER JOIN IdentityUserRoles ur ON ur.RoleId=r.RoleId
            WHERE ur.UserId=@UserId AND ur.ApplicationId=@AppId AND ur.IsActive=1 AND r.IsActive=1 AND p.IsActive=1 AND p.ApplicationId=@AppId";
        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@AppId", applicationId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<IdentityPermission>();
        while (await reader.ReadAsync(ct)) list.Add(PermissionRepository.MapPermission(reader));
        return list;
    }
}
