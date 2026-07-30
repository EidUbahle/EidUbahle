using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using EidUbahle.Domain.DTOs;
using EidUbahle.Domain.Entities;

namespace EidUbahle.Repositories
{
    /// <summary>
    /// Data access for Roles, Permissions and Role-Permission assignments.
    /// All role queries are tenant-scoped; Permissions are global (system-wide).
    /// </summary>
    public class RoleRepository
    {
        private readonly string _conn;

        public RoleRepository(string connectionString)
        {
            _conn = connectionString;
        }

        // ── Permissions (global) ─────────────────────────────────────────

        public List<PermissionDto> GetAllPermissions()
        {
            var list = new List<PermissionDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    SELECT Id, Module, Feature, Action, PermissionKey
                    FROM sys_Permissions
                    ORDER BY Module, Feature, Action";
                using (var cmd = new SqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(MapPermission(r));
            }
            return list;
        }

        public PermissionMatrixDto GetPermissionMatrix()
        {
            var all = GetAllPermissions();
            var matrix = new PermissionMatrixDto();
            var moduleMap = new Dictionary<string, PermissionGroupDto>(StringComparer.OrdinalIgnoreCase);

            foreach (var p in all)
            {
                if (!moduleMap.TryGetValue(p.Module, out var group))
                {
                    group = new PermissionGroupDto { Module = p.Module, Features = new List<PermissionFeatureDto>() };
                    moduleMap[p.Module] = group;
                    matrix.Groups.Add(group);
                }
                var feature = group.Features.Find(f => f.Feature == p.Feature);
                if (feature == null)
                {
                    feature = new PermissionFeatureDto { Feature = p.Feature, Actions = new List<PermissionDto>() };
                    group.Features.Add(feature);
                }
                feature.Actions.Add(p);
            }
            return matrix;
        }

        // ── Roles ─────────────────────────────────────────────────────────

        public PagedResultDto<RoleListItemDto> GetRoles(Guid tenantId, string search, int page = 1, int pageSize = 50)
        {
            var result = new PagedResultDto<RoleListItemDto> { Page = page, PageSize = pageSize };
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var where = "TenantId = @T AND IsDeleted = 0";
                if (!string.IsNullOrWhiteSpace(search))
                    where += " AND (Name LIKE @S OR Description LIKE @S)";

                var countSql = $"SELECT COUNT(1) FROM sys_Roles WHERE {where}";
                using (var cmd = new SqlCommand(countSql, conn))
                {
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@S", $"%{search}%");
                    result.TotalCount = (int)cmd.ExecuteScalar();
                }

                var listSql = $@"
                    SELECT r.Id, r.Name, r.Description, r.IsSystem, r.IsActive, r.CreatedAt,
                        (SELECT COUNT(1) FROM sys_UserRoles ur WHERE ur.RoleId = r.Id) AS UserCount,
                        (SELECT COUNT(1) FROM sys_RolePermissions rp WHERE rp.RoleId = r.Id AND rp.IsGranted = 1) AS PermCount
                    FROM sys_Roles r
                    WHERE {where}
                    ORDER BY r.IsSystem DESC, r.Name
                    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
                using (var cmd = new SqlCommand(listSql, conn))
                {
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@S", $"%{search}%");
                    cmd.Parameters.AddWithValue("@Skip", (page - 1) * pageSize);
                    cmd.Parameters.AddWithValue("@Take", pageSize);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            result.Items.Add(MapRoleListItem(r));
                }
            }
            return result;
        }

        public RoleDetailDto GetById(Guid tenantId, Guid roleId)
        {
            RoleDetailDto dto = null;
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    SELECT r.Id, r.Name, r.Description, r.IsSystem, r.IsActive, r.CreatedAt,
                        (SELECT COUNT(1) FROM sys_UserRoles ur WHERE ur.RoleId = r.Id) AS UserCount,
                        (SELECT COUNT(1) FROM sys_RolePermissions rp WHERE rp.RoleId = r.Id AND rp.IsGranted = 1) AS PermCount
                    FROM sys_Roles r
                    WHERE r.Id = @Id AND r.TenantId = @T AND r.IsDeleted = 0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", roleId);
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read()) dto = new RoleDetailDto
                        {
                            Id = r.GetGuid(0), Name = r.GetString(1),
                            Description = r.IsDBNull(2) ? null : r.GetString(2),
                            IsSystem = r.GetBoolean(3), IsActive = r.GetBoolean(4),
                            CreatedAt = r.GetDateTime(5),
                            UserCount = r.GetInt32(6), PermissionCount = r.GetInt32(7)
                        };
                }
            }
            if (dto == null) return null;
            dto.Permissions = GetRolePermissions(roleId);
            return dto;
        }

        public List<RolePermissionDto> GetRolePermissions(Guid roleId)
        {
            var list = new List<RolePermissionDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    SELECT p.Id, p.Module, p.Feature, p.Action, p.PermissionKey, rp.IsGranted
                    FROM sys_RolePermissions rp
                    JOIN sys_Permissions p ON p.Id = rp.PermissionId
                    WHERE rp.RoleId = @RoleId
                    ORDER BY p.Module, p.Feature, p.Action";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@RoleId", roleId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new RolePermissionDto
                            {
                                PermissionId = r.GetGuid(0),
                                Module = r.GetString(1), Feature = r.GetString(2),
                                Action = r.GetString(3), PermissionKey = r.GetString(4),
                                IsGranted = r.GetBoolean(5)
                            });
                }
            }
            return list;
        }

        public bool NameExists(Guid tenantId, string name, Guid? excludeId = null)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var sql = "SELECT COUNT(1) FROM sys_Roles WHERE TenantId=@T AND Name=@N AND IsDeleted=0";
                if (excludeId.HasValue) sql += " AND Id <> @Ex";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    cmd.Parameters.AddWithValue("@N", name);
                    if (excludeId.HasValue) cmd.Parameters.AddWithValue("@Ex", excludeId.Value);
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        // ── Create ────────────────────────────────────────────────────────

        public Guid Create(Guid tenantId, CreateRoleDto dto)
        {
            var id = Guid.NewGuid();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    const string sql = @"
                        INSERT INTO sys_Roles(Id,TenantId,Name,Description,IsSystem,IsActive,
                            IsDeleted,CreatedAt,UpdatedAt)
                        VALUES(@Id,@T,@Name,@Desc,0,1,0,GETUTCDATE(),GETUTCDATE())";
                    using (var cmd = new SqlCommand(sql, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@T", tenantId);
                        cmd.Parameters.AddWithValue("@Name", dto.Name);
                        cmd.Parameters.AddWithValue("@Desc", (object)dto.Description ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                    SetPermissions(conn, tx, id, dto.PermissionIds);
                    tx.Commit();
                }
            }
            return id;
        }

        // ── Update ────────────────────────────────────────────────────────

        public void Update(Guid tenantId, UpdateRoleDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    const string sql = @"
                        UPDATE sys_Roles SET Name=@Name, Description=@Desc, IsActive=@Active,
                            UpdatedAt=GETUTCDATE()
                        WHERE Id=@Id AND TenantId=@T AND IsSystem=0 AND IsDeleted=0";
                    using (var cmd = new SqlCommand(sql, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Id", dto.Id);
                        cmd.Parameters.AddWithValue("@T", tenantId);
                        cmd.Parameters.AddWithValue("@Name", dto.Name);
                        cmd.Parameters.AddWithValue("@Desc", (object)dto.Description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Active", dto.IsActive);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new SqlCommand(
                        "DELETE FROM sys_RolePermissions WHERE RoleId=@Id", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Id", dto.Id);
                        cmd.ExecuteNonQuery();
                    }
                    SetPermissions(conn, tx, dto.Id, dto.PermissionIds);
                    tx.Commit();
                }
            }
        }

        // ── Soft-delete ───────────────────────────────────────────────────

        public void Delete(Guid tenantId, Guid roleId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "UPDATE sys_Roles SET IsDeleted=1, UpdatedAt=GETUTCDATE() WHERE Id=@Id AND TenantId=@T AND IsSystem=0", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", roleId);
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private void SetPermissions(SqlConnection conn, SqlTransaction tx, Guid roleId, List<Guid> permIds)
        {
            foreach (var pid in permIds)
            {
                using (var cmd = new SqlCommand(
                    "INSERT INTO sys_RolePermissions(Id,RoleId,PermissionId,IsGranted) VALUES(NEWID(),@R,@P,1)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@R", roleId);
                    cmd.Parameters.AddWithValue("@P", pid);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static PermissionDto MapPermission(SqlDataReader r) => new PermissionDto
        {
            Id = r.GetGuid(0), Module = r.GetString(1), Feature = r.GetString(2),
            Action = r.GetString(3), PermissionKey = r.GetString(4)
        };

        private static RoleListItemDto MapRoleListItem(SqlDataReader r) => new RoleListItemDto
        {
            Id = r.GetGuid(0), Name = r.GetString(1),
            Description = r.IsDBNull(2) ? null : r.GetString(2),
            IsSystem = r.GetBoolean(3), IsActive = r.GetBoolean(4),
            CreatedAt = r.GetDateTime(5),
            UserCount = r.GetInt32(6), PermissionCount = r.GetInt32(7)
        };
    }
}
