using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using EidUbahle.Domain.DTOs;
using EidUbahle.Domain.Entities;
using EidUbahle.Infrastructure.Security;

namespace EidUbahle.Repositories
{
    /// <summary>
    /// Data access for users, user-roles and user-branch assignments.
    /// All queries are tenant-scoped.
    /// </summary>
    public class UserRepository
    {
        private readonly string _conn;

        public UserRepository(string connectionString)
        {
            _conn = connectionString;
        }

        // ── List ─────────────────────────────────────────────────────────

        public PagedResultDto<UserListItemDto> GetUsers(Guid tenantId, string search, bool? isActive,
            int page = 1, int pageSize = 20)
        {
            var result = new PagedResultDto<UserListItemDto> { Page = page, PageSize = pageSize };
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var where = "u.TenantId = @TenantId AND u.IsDeleted = 0";
                if (!string.IsNullOrWhiteSpace(search))
                    where += " AND (u.Username LIKE @Search OR u.FullName LIKE @Search OR u.Email LIKE @Search)";
                if (isActive.HasValue)
                    where += " AND u.IsActive = @IsActive";

                var countSql = $"SELECT COUNT(*) FROM sys_Users u WHERE {where}";
                using (var cmd = new SqlCommand(countSql, conn))
                {
                    AddSearchParams(cmd, tenantId, search, isActive);
                    result.TotalCount = (int)cmd.ExecuteScalar();
                }

                var listSql = $@"
                    SELECT u.Id, u.Username, u.FullName, u.Email, u.Phone, u.AvatarUrl,
                           u.IsActive, u.IsTenantAdmin, u.TwoFactorEnabled, u.LastLoginAt, u.CreatedAt
                    FROM sys_Users u
                    WHERE {where}
                    ORDER BY u.FullName, u.Username
                    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
                using (var cmd = new SqlCommand(listSql, conn))
                {
                    AddSearchParams(cmd, tenantId, search, isActive);
                    cmd.Parameters.AddWithValue("@Skip", (page - 1) * pageSize);
                    cmd.Parameters.AddWithValue("@Take", pageSize);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            result.Items.Add(MapListItem(r));
                }
            }

            foreach (var u in result.Items)
            {
                u.RoleNames = GetUserRoleNames(tenantId, u.Id);
                u.BranchNames = GetUserBranchNames(u.Id);
            }

            return result;
        }

        // ── Get by Id ────────────────────────────────────────────────────

        public UserDetailDto GetById(Guid tenantId, Guid userId)
        {
            UserDetailDto dto = null;
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    SELECT u.Id, u.TenantId, u.Username, u.FullName, u.Email, u.Phone, u.AvatarUrl,
                           u.IsActive, u.IsTenantAdmin, u.IsSuperAdmin, u.TwoFactorEnabled,
                           u.LanguageCode, u.ThemeMode, u.ActiveLayout, u.AccentColor,
                           u.FailedLoginAttempts, u.LockedUntil, u.LastLoginAt, u.CreatedAt, u.UpdatedAt
                    FROM sys_Users u
                    WHERE u.Id = @Id AND u.TenantId = @TenantId AND u.IsDeleted = 0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", userId);
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read()) dto = MapDetail(r);
                }
            }
            if (dto == null) return null;
            dto.Roles = GetUserRoleAssignments(userId);
            dto.Branches = GetUserBranchAssignments(userId);
            dto.RoleNames = GetUserRoleNames(tenantId, userId);
            dto.BranchNames = GetUserBranchNames(userId);
            return dto;
        }

        public AppUser GetAppUserById(Guid userId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    SELECT Id, TenantId, Username, Email, PasswordHash, PasswordSalt,
                           FullName, AvatarUrl, Phone, LanguageCode, ThemeMode, ActiveLayout, AccentColor,
                           IsTenantAdmin, IsSuperAdmin, IsActive, TwoFactorEnabled, TwoFactorSecret,
                           FailedLoginAttempts, LockedUntil, LastLoginAt
                    FROM sys_Users WHERE Id = @Id AND IsDeleted = 0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", userId);
                    using (var r = cmd.ExecuteReader())
                        return r.Read() ? MapAppUser(r) : null;
                }
            }
        }

        public bool UsernameExists(Guid tenantId, string username, Guid? excludeId = null)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var sql = "SELECT COUNT(1) FROM sys_Users WHERE TenantId=@T AND Username=@U AND IsDeleted=0";
                if (excludeId.HasValue) sql += " AND Id <> @Ex";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    cmd.Parameters.AddWithValue("@U", username);
                    if (excludeId.HasValue) cmd.Parameters.AddWithValue("@Ex", excludeId.Value);
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        public bool EmailExists(Guid tenantId, string email, Guid? excludeId = null)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var sql = "SELECT COUNT(1) FROM sys_Users WHERE TenantId=@T AND Email=@E AND IsDeleted=0";
                if (excludeId.HasValue) sql += " AND Id <> @Ex";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    cmd.Parameters.AddWithValue("@E", email);
                    if (excludeId.HasValue) cmd.Parameters.AddWithValue("@Ex", excludeId.Value);
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        public int CountActiveUsers(Guid tenantId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(1) FROM sys_Users WHERE TenantId=@T AND IsDeleted=0 AND IsActive=1", conn))
                {
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        // ── Create ────────────────────────────────────────────────────────

        public Guid Create(Guid tenantId, CreateUserDto dto, string passwordHash, string passwordSalt)
        {
            var id = Guid.NewGuid();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    const string sql = @"
                        INSERT INTO sys_Users(Id,TenantId,Username,Email,PasswordHash,PasswordSalt,
                            FullName,Phone,LanguageCode,IsTenantAdmin,IsActive,CreatedAt,UpdatedAt,IsDeleted)
                        VALUES(@Id,@TenantId,@Username,@Email,@Hash,@Salt,
                            @FullName,@Phone,@Lang,@IsAdmin,1,GETUTCDATE(),GETUTCDATE(),0)";
                    using (var cmd = new SqlCommand(sql, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@TenantId", tenantId);
                        cmd.Parameters.AddWithValue("@Username", dto.Username);
                        cmd.Parameters.AddWithValue("@Email", (object)dto.Email ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Hash", passwordHash);
                        cmd.Parameters.AddWithValue("@Salt", passwordSalt);
                        cmd.Parameters.AddWithValue("@FullName", (object)dto.FullName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Phone", (object)dto.Phone ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Lang", dto.LanguageCode ?? "en");
                        cmd.Parameters.AddWithValue("@IsAdmin", dto.IsTenantAdmin);
                        cmd.ExecuteNonQuery();
                    }
                    AssignRoles(conn, tx, id, dto.RoleIds);
                    AssignBranches(conn, tx, id, dto.Branches);
                    tx.Commit();
                }
            }
            return id;
        }

        // ── Update ────────────────────────────────────────────────────────

        public void Update(Guid tenantId, UpdateUserDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    const string sql = @"
                        UPDATE sys_Users SET
                            FullName=@FullName, Email=@Email, Phone=@Phone, AvatarUrl=@AvatarUrl,
                            IsTenantAdmin=@IsAdmin, IsActive=@IsActive, LanguageCode=@Lang,
                            ThemeMode=@Theme, ActiveLayout=@Layout, AccentColor=@Accent,
                            UpdatedAt=GETUTCDATE()
                        WHERE Id=@Id AND TenantId=@TenantId AND IsDeleted=0";
                    using (var cmd = new SqlCommand(sql, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Id", dto.Id);
                        cmd.Parameters.AddWithValue("@TenantId", tenantId);
                        cmd.Parameters.AddWithValue("@FullName", (object)dto.FullName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", (object)dto.Email ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Phone", (object)dto.Phone ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@AvatarUrl", (object)dto.AvatarUrl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsAdmin", dto.IsTenantAdmin);
                        cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);
                        cmd.Parameters.AddWithValue("@Lang", (object)dto.LanguageCode ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Theme", (object)dto.ThemeMode ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Layout", (object)dto.ActiveLayout ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Accent", (object)dto.AccentColor ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }

                    // replace role assignments
                    using (var cmd = new SqlCommand(
                        "DELETE FROM sys_UserRoles WHERE UserId=@Id", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Id", dto.Id);
                        cmd.ExecuteNonQuery();
                    }
                    AssignRoles(conn, tx, dto.Id, dto.RoleIds);

                    // replace branch assignments
                    using (var cmd = new SqlCommand(
                        "DELETE FROM sys_UserCompanyBranches WHERE UserId=@Id", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Id", dto.Id);
                        cmd.ExecuteNonQuery();
                    }
                    AssignBranches(conn, tx, dto.Id, dto.Branches);
                    tx.Commit();
                }
            }
        }

        // ── Password ─────────────────────────────────────────────────────

        public void UpdatePassword(Guid userId, string hash, string salt)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "UPDATE sys_Users SET PasswordHash=@H, PasswordSalt=@S, UpdatedAt=GETUTCDATE() WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@H", hash);
                    cmd.Parameters.AddWithValue("@S", salt);
                    cmd.Parameters.AddWithValue("@Id", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ── Soft-delete ───────────────────────────────────────────────────

        public void Delete(Guid tenantId, Guid userId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "UPDATE sys_Users SET IsDeleted=1, IsActive=0, UpdatedAt=GETUTCDATE() WHERE Id=@Id AND TenantId=@T", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", userId);
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ── Unlock ────────────────────────────────────────────────────────

        public void Unlock(Guid tenantId, Guid userId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "UPDATE sys_Users SET LockedUntil=NULL, FailedLoginAttempts=0 WHERE Id=@Id AND TenantId=@T", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", userId);
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ── Invitations ───────────────────────────────────────────────────

        public Guid CreateInvitation(Guid tenantId, Guid invitedBy, string email, string fullName,
            string token, DateTime expiresAt, List<Guid> roleIds, List<UserBranchAssignmentDto> branches)
        {
            var id = Guid.NewGuid();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    const string sql = @"
                        INSERT INTO sys_Invitations(Id,TenantId,InvitedBy,Email,FullName,Token,Status,ExpiresAt,RoleIds,CreatedAt)
                        VALUES(@Id,@TenantId,@InvitedBy,@Email,@FullName,@Token,'Pending',@Exp,@Roles,GETUTCDATE())";
                    using (var cmd = new SqlCommand(sql, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@TenantId", tenantId);
                        cmd.Parameters.AddWithValue("@InvitedBy", invitedBy);
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@FullName", (object)fullName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Token", token);
                        cmd.Parameters.AddWithValue("@Exp", expiresAt);
                        var roleJson = new System.Web.Script.Serialization.JavaScriptSerializer()
                            .Serialize(roleIds);
                        cmd.Parameters.AddWithValue("@Roles", roleJson);
                        cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
            }
            return id;
        }

        public InvitationDto GetInvitationByToken(string token)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    SELECT i.Id, i.Email, i.FullName, i.Status, i.ExpiresAt, i.CreatedAt,
                           u.FullName AS InvitedByName
                    FROM sys_Invitations i
                    LEFT JOIN sys_Users u ON u.Id = i.InvitedBy
                    WHERE i.Token = @Token";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Token", token);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read()) return new InvitationDto
                        {
                            Id = r.GetGuid(0),
                            Email = r.GetString(1),
                            FullName = r.IsDBNull(2) ? null : r.GetString(2),
                            Status = r.GetString(3),
                            ExpiresAt = r.GetDateTime(4),
                            CreatedAt = r.GetDateTime(5),
                            InvitedByName = r.IsDBNull(6) ? null : r.GetString(6)
                        };
                }
            }
            return null;
        }

        public void AcceptInvitation(Guid invitationId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "UPDATE sys_Invitations SET Status='Accepted', AcceptedAt=GETUTCDATE() WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invitationId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<InvitationDto> GetInvitations(Guid tenantId)
        {
            var list = new List<InvitationDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    SELECT i.Id, i.Email, i.FullName, i.Status, i.ExpiresAt, i.CreatedAt,
                           u.FullName AS InvitedByName
                    FROM sys_Invitations i
                    LEFT JOIN sys_Users u ON u.Id = i.InvitedBy
                    WHERE i.TenantId = @T
                    ORDER BY i.CreatedAt DESC";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new InvitationDto
                            {
                                Id = r.GetGuid(0),
                                Email = r.GetString(1),
                                FullName = r.IsDBNull(2) ? null : r.GetString(2),
                                Status = r.GetString(3),
                                ExpiresAt = r.GetDateTime(4),
                                CreatedAt = r.GetDateTime(5),
                                InvitedByName = r.IsDBNull(6) ? null : r.GetString(6)
                            });
                }
            }
            return list;
        }

        // ── Private helpers ───────────────────────────────────────────────

        private void AssignRoles(SqlConnection conn, SqlTransaction tx, Guid userId, List<Guid> roleIds)
        {
            foreach (var rid in roleIds)
            {
                using (var cmd = new SqlCommand(
                    "INSERT INTO sys_UserRoles(Id,UserId,RoleId,AssignedAt) VALUES(NEWID(),@U,@R,GETUTCDATE())", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@U", userId);
                    cmd.Parameters.AddWithValue("@R", rid);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void AssignBranches(SqlConnection conn, SqlTransaction tx, Guid userId,
            List<UserBranchAssignmentDto> branches)
        {
            foreach (var b in branches)
            {
                using (var cmd = new SqlCommand(
                    "INSERT INTO sys_UserCompanyBranches(Id,UserId,CompanyId,BranchId) VALUES(NEWID(),@U,@C,@B)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@U", userId);
                    cmd.Parameters.AddWithValue("@C", b.CompanyId);
                    cmd.Parameters.AddWithValue("@B", b.BranchId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private List<string> GetUserRoleNames(Guid tenantId, Guid userId)
        {
            var list = new List<string>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    SELECT r.Name FROM sys_UserRoles ur
                    JOIN sys_Roles r ON r.Id = ur.RoleId
                    WHERE ur.UserId = @U AND r.TenantId = @T AND r.IsDeleted = 0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@U", userId);
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(r.GetString(0));
                }
            }
            return list;
        }

        private List<string> GetUserBranchNames(Guid userId)
        {
            var list = new List<string>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    SELECT b.Name FROM sys_UserCompanyBranches ucb
                    JOIN saas_Branches b ON b.Id = ucb.BranchId
                    WHERE ucb.UserId = @U AND b.IsDeleted = 0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@U", userId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(r.GetString(0));
                }
            }
            return list;
        }

        private List<UserRoleAssignmentDto> GetUserRoleAssignments(Guid userId)
        {
            var list = new List<UserRoleAssignmentDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    SELECT ur.Id, ur.RoleId, r.Name, ur.CompanyId, c.Name, ur.BranchId, b.Name
                    FROM sys_UserRoles ur
                    JOIN sys_Roles r ON r.Id = ur.RoleId
                    LEFT JOIN saas_Companies c ON c.Id = ur.CompanyId
                    LEFT JOIN saas_Branches b ON b.Id = ur.BranchId
                    WHERE ur.UserId = @U";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@U", userId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new UserRoleAssignmentDto
                            {
                                UserRoleId = r.GetGuid(0),
                                RoleId = r.GetGuid(1),
                                RoleName = r.GetString(2),
                                CompanyId = r.IsDBNull(3) ? (Guid?)null : r.GetGuid(3),
                                CompanyName = r.IsDBNull(4) ? null : r.GetString(4),
                                BranchId = r.IsDBNull(5) ? (Guid?)null : r.GetGuid(5),
                                BranchName = r.IsDBNull(6) ? null : r.GetString(6)
                            });
                }
            }
            return list;
        }

        private List<UserBranchAssignmentDto> GetUserBranchAssignments(Guid userId)
        {
            var list = new List<UserBranchAssignmentDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    SELECT ucb.CompanyId, c.Name, ucb.BranchId, b.Name, b.Code
                    FROM sys_UserCompanyBranches ucb
                    JOIN saas_Companies c ON c.Id = ucb.CompanyId
                    JOIN saas_Branches b ON b.Id = ucb.BranchId
                    WHERE ucb.UserId = @U AND c.IsDeleted=0 AND b.IsDeleted=0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@U", userId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new UserBranchAssignmentDto
                            {
                                CompanyId = r.GetGuid(0), CompanyName = r.GetString(1),
                                BranchId = r.GetGuid(2), BranchName = r.GetString(3),
                                BranchCode = r.IsDBNull(4) ? null : r.GetString(4)
                            });
                }
            }
            return list;
        }

        private static void AddSearchParams(SqlCommand cmd, Guid tenantId, string search, bool? isActive)
        {
            cmd.Parameters.AddWithValue("@TenantId", tenantId);
            if (!string.IsNullOrWhiteSpace(search))
                cmd.Parameters.AddWithValue("@Search", $"%{search}%");
            if (isActive.HasValue)
                cmd.Parameters.AddWithValue("@IsActive", isActive.Value);
        }

        private static UserListItemDto MapListItem(SqlDataReader r) => new UserListItemDto
        {
            Id = r.GetGuid(0), Username = r.GetString(1),
            FullName = r.IsDBNull(2) ? null : r.GetString(2),
            Email = r.IsDBNull(3) ? null : r.GetString(3),
            Phone = r.IsDBNull(4) ? null : r.GetString(4),
            AvatarUrl = r.IsDBNull(5) ? null : r.GetString(5),
            IsActive = r.GetBoolean(6), IsTenantAdmin = r.GetBoolean(7),
            TwoFactorEnabled = r.GetBoolean(8),
            LastLoginAt = r.IsDBNull(9) ? (DateTime?)null : r.GetDateTime(9),
            CreatedAt = r.GetDateTime(10)
        };

        private static UserDetailDto MapDetail(SqlDataReader r) => new UserDetailDto
        {
            Id = r.GetGuid(0), TenantId = r.GetGuid(1), Username = r.GetString(2),
            FullName = r.IsDBNull(3) ? null : r.GetString(3),
            Email = r.IsDBNull(4) ? null : r.GetString(4),
            Phone = r.IsDBNull(5) ? null : r.GetString(5),
            AvatarUrl = r.IsDBNull(6) ? null : r.GetString(6),
            IsActive = r.GetBoolean(7), IsTenantAdmin = r.GetBoolean(8),
            IsSuperAdmin = r.GetBoolean(9), TwoFactorEnabled = r.GetBoolean(10),
            LanguageCode = r.IsDBNull(11) ? "en" : r.GetString(11),
            ThemeMode = r.IsDBNull(12) ? "auto" : r.GetString(12),
            ActiveLayout = r.IsDBNull(13) ? null : r.GetString(13),
            AccentColor = r.IsDBNull(14) ? null : r.GetString(14),
            FailedLoginAttempts = r.GetInt32(15),
            LockedUntil = r.IsDBNull(16) ? (DateTime?)null : r.GetDateTime(16),
            LastLoginAt = r.IsDBNull(17) ? (DateTime?)null : r.GetDateTime(17),
            CreatedAt = r.GetDateTime(18), UpdatedAt = r.GetDateTime(19)
        };

        private static AppUser MapAppUser(SqlDataReader r) => new AppUser
        {
            Id = r.GetGuid(0), TenantId = r.GetGuid(1), Username = r.GetString(2),
            Email = r.IsDBNull(3) ? null : r.GetString(3),
            PasswordHash = r.IsDBNull(4) ? null : r.GetString(4),
            PasswordSalt = r.IsDBNull(5) ? null : r.GetString(5),
            FullName = r.IsDBNull(6) ? null : r.GetString(6),
            AvatarUrl = r.IsDBNull(7) ? null : r.GetString(7),
            Phone = r.IsDBNull(8) ? null : r.GetString(8),
            LanguageCode = r.IsDBNull(9) ? "en" : r.GetString(9),
            ThemeMode = r.IsDBNull(10) ? "auto" : r.GetString(10),
            ActiveLayout = r.IsDBNull(11) ? null : r.GetString(11),
            AccentColor = r.IsDBNull(12) ? null : r.GetString(12),
            IsTenantAdmin = r.GetBoolean(13), IsSuperAdmin = r.GetBoolean(14),
            IsActive = r.GetBoolean(15), TwoFactorEnabled = r.GetBoolean(16),
            TwoFactorSecret = r.IsDBNull(17) ? null : r.GetString(17),
            FailedLoginAttempts = r.GetInt32(18),
            LockedUntil = r.IsDBNull(19) ? (DateTime?)null : r.GetDateTime(19),
            LastLoginAt = r.IsDBNull(20) ? (DateTime?)null : r.GetDateTime(20)
        };
    }
}
