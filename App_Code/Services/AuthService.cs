using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using EidUbahle.Domain.DTOs;
using EidUbahle.Domain.Entities;
using EidUbahle.Infrastructure.Caching;
using EidUbahle.Infrastructure.Security;

namespace EidUbahle.Services
{
    /// <summary>
    /// Authentication and session management service.
    /// Decoupled from the WebForms presentation layer – usable from any future API.
    /// </summary>
    public class AuthService
    {
        private readonly string _conn;
        private readonly IAppCache _cache;

        public AuthService(string connectionString, IAppCache cache)
        {
            _conn = connectionString;
            _cache = cache;
        }

        // ── Login ────────────────────────────────────────────────────────
        public LoginResponseDto Login(LoginRequestDto request, string ipAddress, string userAgent)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return Fail("ERR_EMPTY_CREDENTIALS", "Username and password required");

            var user = GetUserByUsername(request.Username);
            if (user == null)
                return Fail("ERR_INVALID_CREDENTIALS", "Invalid username or password");

            // Lockout check
            if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
                return Fail("ERR_ACCOUNT_LOCKED", $"Account locked until {user.LockedUntil:HH:mm} UTC");

            if (!user.IsActive)
                return Fail("ERR_ACCOUNT_INACTIVE", "Account is inactive");

            // Password verification
            if (!PasswordService.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                IncrementFailedAttempts(user);
                RecordLogin(user.Id, ipAddress, userAgent, false, "Invalid password");
                return Fail("ERR_INVALID_CREDENTIALS", "Invalid username or password");
            }

            // 2FA
            if (user.TwoFactorEnabled)
            {
                if (string.IsNullOrWhiteSpace(request.TotpCode))
                    return new LoginResponseDto { Success = false, Require2FA = true };

                if (!PasswordService.VerifyTotp(user.TwoFactorSecret, request.TotpCode))
                {
                    RecordLogin(user.Id, ipAddress, userAgent, false, "Invalid 2FA code");
                    return Fail("ERR_INVALID_2FA", "Invalid 2FA code");
                }
            }

            // Build claims
            var claims = BuildClaims(user);

            // Tokens
            var accessToken = JwtService.GenerateAccessToken(claims);
            var refreshToken = JwtService.GenerateRefreshToken();
            var refreshExpiry = request.RememberMe
                ? DateTime.UtcNow.AddDays(ConfigHelper.JwtRefreshTokenDays)
                : DateTime.UtcNow.AddDays(1);

            SaveRefreshToken(user.Id, refreshToken, request.DeviceId, request.DeviceInfo, ipAddress, refreshExpiry);
            ResetFailedAttempts(user.Id);
            RecordLogin(user.Id, ipAddress, userAgent, true, null);
            UpdateLastLogin(user.Id);

            return new LoginResponseDto
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(ConfigHelper.JwtAccessTokenMinutes),
                RefreshTokenExpiry = refreshExpiry,
                UserClaims = claims
            };
        }

        // ── Refresh Token ────────────────────────────────────────────────
        public LoginResponseDto RefreshToken(RefreshTokenRequestDto request, string ipAddress)
        {
            var tokenRow = GetRefreshToken(request.RefreshToken);
            if (tokenRow == null || tokenRow.IsRevoked || tokenRow.ExpiresAt < DateTime.UtcNow)
                return Fail("ERR_INVALID_REFRESH_TOKEN", "Invalid or expired refresh token");

            var user = GetUserById(tokenRow.UserId);
            if (user == null || !user.IsActive)
                return Fail("ERR_USER_INACTIVE", "User account is inactive");

            var claims = BuildClaims(user);
            var newAccess = JwtService.GenerateAccessToken(claims);
            var newRefresh = JwtService.GenerateRefreshToken();

            RevokeRefreshToken(request.RefreshToken);
            SaveRefreshToken(user.Id, newRefresh, request.DeviceId, null, ipAddress,
                DateTime.UtcNow.AddDays(ConfigHelper.JwtRefreshTokenDays));

            return new LoginResponseDto
            {
                Success = true,
                AccessToken = newAccess,
                RefreshToken = newRefresh,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(ConfigHelper.JwtAccessTokenMinutes),
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(ConfigHelper.JwtRefreshTokenDays),
                UserClaims = claims
            };
        }

        // ── Logout ───────────────────────────────────────────────────────
        public void Logout(string refreshToken)
        {
            if (!string.IsNullOrEmpty(refreshToken))
                RevokeRefreshToken(refreshToken);
        }

        // ── Build user claims ─────────────────────────────────────────────
        private UserClaimsDto BuildClaims(AppUser user)
        {
            var permissions = GetUserPermissions(user.Id, user.TenantId);
            var branches = GetUserBranches(user.Id);
            var firstBranch = branches.FirstOrDefault();

            return new UserClaimsDto
            {
                UserId = user.Id,
                TenantId = user.TenantId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                LanguageCode = user.LanguageCode ?? "en",
                ThemeMode = user.ThemeMode ?? "auto",
                ActiveLayout = user.ActiveLayout,
                AccentColor = user.AccentColor,
                IsTenantAdmin = user.IsTenantAdmin,
                IsSuperAdmin = user.IsSuperAdmin,
                Permissions = permissions,
                CompanyBranches = branches,
                ActiveCompanyId = firstBranch?.CompanyId,
                ActiveBranchId = firstBranch?.BranchId,
                ActiveCompanyName = firstBranch?.CompanyName,
                ActiveBranchName = firstBranch?.BranchName
            };
        }

        // ── DB Helpers ───────────────────────────────────────────────────
        private AppUser GetUserByUsername(string username)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"SELECT Id,TenantId,Username,Email,PasswordHash,PasswordSalt,FullName,
                                            AvatarUrl,Phone,LanguageCode,ThemeMode,ActiveLayout,AccentColor,
                                            IsTenantAdmin,IsSuperAdmin,IsActive,TwoFactorEnabled,TwoFactorSecret,
                                            FailedLoginAttempts,LockedUntil,LastLoginAt
                                     FROM sys_Users
                                     WHERE (Username=@U OR Email=@U) AND IsDeleted=0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@U", username);
                    using (var r = cmd.ExecuteReader())
                        return r.Read() ? MapUser(r) : null;
                }
            }
        }

        private AppUser GetUserById(Guid id)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"SELECT Id,TenantId,Username,Email,PasswordHash,PasswordSalt,FullName,
                                            AvatarUrl,Phone,LanguageCode,ThemeMode,ActiveLayout,AccentColor,
                                            IsTenantAdmin,IsSuperAdmin,IsActive,TwoFactorEnabled,TwoFactorSecret,
                                            FailedLoginAttempts,LockedUntil,LastLoginAt
                                     FROM sys_Users WHERE Id=@Id AND IsDeleted=0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var r = cmd.ExecuteReader())
                        return r.Read() ? MapUser(r) : null;
                }
            }
        }

        private AppUser MapUser(SqlDataReader r) => new AppUser
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

        private List<string> GetUserPermissions(Guid userId, Guid tenantId)
        {
            var cacheKey = $"perms:{userId}";
            return _cache.GetOrAdd(cacheKey, () =>
            {
                var list = new List<string>();
                using (var conn = new SqlConnection(_conn))
                {
                    conn.Open();
                    const string sql = @"
                        SELECT DISTINCT p.PermissionKey
                        FROM sys_RolePermissions rp
                        JOIN sys_Permissions p ON p.Id = rp.PermissionId
                        JOIN sys_UserRoles ur ON ur.RoleId = rp.RoleId
                        WHERE ur.UserId = @UserId AND rp.IsGranted = 1";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        using (var r = cmd.ExecuteReader())
                            while (r.Read()) list.Add(r.GetString(0));
                    }
                }
                return list;
            }, TimeSpan.FromMinutes(10));
        }

        private List<CompanyBranchDto> GetUserBranches(Guid userId)
        {
            var list = new List<CompanyBranchDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    SELECT ucb.CompanyId, c.Name, ucb.BranchId, b.Name, b.Code
                    FROM sys_UserCompanyBranches ucb
                    JOIN saas_Companies c ON c.Id = ucb.CompanyId
                    JOIN saas_Branches b ON b.Id = ucb.BranchId
                    WHERE ucb.UserId = @UserId AND c.IsDeleted=0 AND b.IsDeleted=0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new CompanyBranchDto
                            {
                                CompanyId = r.GetGuid(0), CompanyName = r.GetString(1),
                                BranchId = r.GetGuid(2), BranchName = r.GetString(3),
                                BranchCode = r.IsDBNull(4) ? null : r.GetString(4)
                            });
                }
            }
            return list;
        }

        private RefreshToken GetRefreshToken(string token)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = "SELECT Id,UserId,Token,DeviceId,ExpiresAt,IsRevoked FROM sys_RefreshTokens WHERE Token=@T";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@T", token);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read()) return new RefreshToken
                        {
                            Id = r.GetGuid(0), UserId = r.GetGuid(1), Token = r.GetString(2),
                            DeviceId = r.IsDBNull(3) ? null : r.GetString(3),
                            ExpiresAt = r.GetDateTime(4), IsRevoked = r.GetBoolean(5)
                        };
                }
            }
            return null;
        }

        private void SaveRefreshToken(Guid userId, string token, string deviceId, string deviceInfo, string ip, DateTime expires)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"INSERT INTO sys_RefreshTokens(Id,UserId,Token,DeviceId,DeviceInfo,IpAddress,ExpiresAt,IsRevoked,CreatedAt)
                                     VALUES(NEWID(),@UserId,@Token,@DeviceId,@DeviceInfo,@Ip,@Expires,0,GETUTCDATE())";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Token", token);
                    cmd.Parameters.AddWithValue("@DeviceId", (object)deviceId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DeviceInfo", (object)deviceInfo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Ip", (object)ip ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Expires", expires);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void RevokeRefreshToken(string token)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand("UPDATE sys_RefreshTokens SET IsRevoked=1 WHERE Token=@T", conn))
                {
                    cmd.Parameters.AddWithValue("@T", token);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void IncrementFailedAttempts(AppUser user)
        {
            int newCount = user.FailedLoginAttempts + 1;
            DateTime? lockUntil = newCount >= ConfigHelper.MaxLoginAttempts
                ? DateTime.UtcNow.AddMinutes(ConfigHelper.LockoutMinutes) : (DateTime?)null;
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"UPDATE sys_Users SET FailedLoginAttempts=@C, LockedUntil=@L WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@C", newCount);
                    cmd.Parameters.AddWithValue("@L", (object)lockUntil ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Id", user.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void ResetFailedAttempts(Guid userId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand("UPDATE sys_Users SET FailedLoginAttempts=0, LockedUntil=NULL WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void UpdateLastLogin(Guid userId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand("UPDATE sys_Users SET LastLoginAt=GETUTCDATE() WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void RecordLogin(Guid userId, string ip, string agent, bool success, string reason)
        {
            try
            {
                using (var conn = new SqlConnection(_conn))
                {
                    conn.Open();
                    const string sql = @"INSERT INTO sys_LoginHistory(Id,UserId,IpAddress,UserAgent,Success,FailureReason,AttemptedAt)
                                         VALUES(NEWID(),@U,@Ip,@Ag,@S,@R,GETUTCDATE())";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@U", userId);
                        cmd.Parameters.AddWithValue("@Ip", (object)ip ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Ag", (object)agent ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@S", success);
                        cmd.Parameters.AddWithValue("@R", (object)reason ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { /* login history is non-critical; don't fail the login */ }
        }

        private static LoginResponseDto Fail(string code, string msg) =>
            new LoginResponseDto { Success = false, ErrorCode = code, ErrorMessage = msg };
    }
}
