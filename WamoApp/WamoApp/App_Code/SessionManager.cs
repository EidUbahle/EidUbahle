using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Security;

namespace WamoApp
{
    public static class SessionManager
    {
        public static bool IsAuthenticated() => HttpContext.Current != null && HttpContext.Current.User != null && HttpContext.Current.User.Identity != null && HttpContext.Current.User.Identity.IsAuthenticated;
        public static string GetCurrentUserName() => IsAuthenticated() ? HttpContext.Current.User.Identity.Name : string.Empty;

        public static int GetCurrentUserId()
        {
            if (!IsAuthenticated()) throw new UnauthorizedAccessException("Authentication required.");
            var identity = HttpContext.Current.User.Identity as FormsIdentity;
            if (identity == null) throw new UnauthorizedAccessException("Authentication required.");
            return int.Parse((identity.Ticket.UserData ?? "0").Split('|')[0]);
        }

        public static int? GetCurrentUserIdOrNull() => IsAuthenticated() ? (int?)GetCurrentUserId() : null;
        public static bool IsInRole(string roleName) => HttpContext.Current != null && HttpContext.Current.User != null && HttpContext.Current.User.IsInRole(roleName);

        public static void AttachPrincipalFromCookie()
        {
            var context = HttpContext.Current; if (context == null) return;
            var authCookie = context.Request.Cookies[ConfigurationManager.AppSettings["AuthCookieName"] ?? FormsAuthentication.FormsCookieName];
            if (authCookie == null || string.IsNullOrWhiteSpace(authCookie.Value)) return;
            try
            {
                var ticket = FormsAuthentication.Decrypt(authCookie.Value); if (ticket == null || ticket.Expired) return;
                var parts = (ticket.UserData ?? string.Empty).Split('|');
                var roles = parts.Length > 1 ? parts[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) : new string[0];
                context.User = new System.Security.Principal.GenericPrincipal(new FormsIdentity(ticket), roles);
            }
            catch { }
        }

        public static string CreateSession(int userId, string username, string roleList, bool rememberMe)
        {
            var tokenBytes = new byte[48];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            var token = Convert.ToBase64String(tokenBytes);
            var sessionId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var expiry = rememberMe ? (object)now.AddDays(GetRememberMeDays()) : DBNull.Value;
            DbHelper.ExecuteNonQuery(@"INSERT INTO UserSessions (SessionID, UserID, SessionTokenHash, LoginTime, LastActivity, ExpirationTime, IPAddress, UserAgent, DeviceName, Browser, OperatingSystem, IsActive, IsRevoked) VALUES (@SessionID,@UserID,@SessionTokenHash,@LoginTime,@LastActivity,@ExpirationTime,@IPAddress,@UserAgent,@DeviceName,@Browser,@OperatingSystem,1,0)", CommandType.Text, new SqlParameter("@SessionID", sessionId), new SqlParameter("@UserID", userId), new SqlParameter("@SessionTokenHash", PasswordHasher.HashPassword(token)), new SqlParameter("@LoginTime", now), new SqlParameter("@LastActivity", now), new SqlParameter("@ExpirationTime", expiry), new SqlParameter("@IPAddress", SecurityHelper.GetIpAddress()), new SqlParameter("@UserAgent", SecurityHelper.GetUserAgent()), new SqlParameter("@DeviceName", HttpContext.Current.Request.Browser.Platform), new SqlParameter("@Browser", HttpContext.Current.Request.Browser.Browser), new SqlParameter("@OperatingSystem", HttpContext.Current.Request.Browser.Platform));
            var ticket = new FormsAuthenticationTicket(1, username, DateTime.Now, DateTime.Now.AddDays(rememberMe ? GetRememberMeDays() : 1), rememberMe, userId + "|" + roleList);
            HttpContext.Current.Response.Cookies.Set(new HttpCookie(ConfigurationManager.AppSettings["AuthCookieName"] ?? FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket)) { HttpOnly = true, Secure = HttpContext.Current.Request.IsSecureConnection || SecurityHelper.IsHttpsRequired(), SameSite = SameSiteMode.Lax, Expires = rememberMe ? DateTime.UtcNow.AddDays(GetRememberMeDays()) : DateTime.MinValue });
            HttpContext.Current.Response.Cookies.Set(new HttpCookie(GetSessionCookieName(), token) { HttpOnly = true, Secure = HttpContext.Current.Request.IsSecureConnection || SecurityHelper.IsHttpsRequired(), SameSite = SameSiteMode.Lax, Expires = rememberMe ? DateTime.UtcNow.AddDays(GetRememberMeDays()) : DateTime.MinValue });
            return token;
        }

        public static void RequireAuthenticated() { if (!IsAuthenticated()) throw new UnauthorizedAccessException("Authentication required."); ValidateCurrentRequest(); }

        public static void ValidateCurrentRequest()
        {
            if (!IsAuthenticated()) return;
            var cookie = HttpContext.Current.Request.Cookies[GetSessionCookieName()];
            if (cookie == null || string.IsNullOrWhiteSpace(cookie.Value)) { ForceLogout(); throw new UnauthorizedAccessException("Session missing."); }
            var table = DbHelper.ExecuteDataTable(@"SELECT TOP 50 SessionID, SessionTokenHash, LastActivity, ExpirationTime, IsActive, IsRevoked FROM UserSessions WHERE UserID = @UserID AND IsActive = 1 ORDER BY LastActivity DESC", CommandType.Text, new SqlParameter("@UserID", GetCurrentUserId()));
            if (table.Rows.Count == 0) { ForceLogout(); throw new UnauthorizedAccessException("Session not found."); }
            DataRow matchedRow = null;
            foreach (DataRow candidate in table.Rows)
            {
                if (PasswordHasher.VerifyPassword(cookie.Value, candidate["SessionTokenHash"].ToString()))
                {
                    matchedRow = candidate;
                    break;
                }
            }
            if (matchedRow == null) { ForceLogout(); throw new UnauthorizedAccessException("Invalid session token."); }
            if (Convert.ToBoolean(matchedRow["IsRevoked"]) || !Convert.ToBoolean(matchedRow["IsActive"])) { ForceLogout(); throw new UnauthorizedAccessException("Session revoked."); }
            var expiry = matchedRow["ExpirationTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(matchedRow["ExpirationTime"]);
            if (expiry.HasValue && expiry.Value < DateTime.UtcNow) { RevokeSession(Convert.ToGuid(matchedRow["SessionID"]), GetCurrentUserId(), "Expired"); ForceLogout(); throw new UnauthorizedAccessException("Session expired."); }
            if (Convert.ToDateTime(matchedRow["LastActivity"]) < DateTime.UtcNow.AddMinutes(-GetInactivityMinutes())) { RevokeSession(Convert.ToGuid(matchedRow["SessionID"]), GetCurrentUserId(), "Inactivity timeout"); ForceLogout(); throw new UnauthorizedAccessException("Session timed out."); }
            DbHelper.ExecuteNonQuery("UPDATE UserSessions SET LastActivity = GETUTCDATE() WHERE SessionID = @SessionID", CommandType.Text, new SqlParameter("@SessionID", Convert.ToGuid(matchedRow["SessionID"])));
        }

        public static void LogoutCurrentUser()
        {
            if (IsAuthenticated())
            {
                DbHelper.ExecuteNonQuery("UPDATE UserSessions SET IsActive = 0, LogoutTime = GETUTCDATE(), LastActivity = GETUTCDATE() WHERE UserID = @UserID AND IsActive = 1", CommandType.Text, new SqlParameter("@UserID", GetCurrentUserId()));
                AuditLogger.Log(GetCurrentUserId(), "LOGOUT", "Authentication", HttpContext.Current.Request.RawUrl, null, null, null);
            }
            ForceLogout();
        }

        public static void RevokeUserSessions(int userId, int revokedBy, string reason)
        {
            DbHelper.ExecuteNonQuery("UPDATE UserSessions SET IsActive = 0, IsRevoked = 1, RevokedAt = GETUTCDATE(), RevokedBy = @RevokedBy WHERE UserID = @UserID AND IsActive = 1", CommandType.Text, new SqlParameter("@RevokedBy", revokedBy), new SqlParameter("@UserID", userId));
            AuditLogger.Log(revokedBy, "SESSION_REVOKE", "Sessions", "Admin/Sessions.aspx", userId.ToString(), null, reason);
        }

        public static void RevokeSession(Guid sessionId, int revokedBy, string reason)
        {
            DbHelper.ExecuteNonQuery("UPDATE UserSessions SET IsActive = 0, IsRevoked = 1, RevokedAt = GETUTCDATE(), RevokedBy = @RevokedBy WHERE SessionID = @SessionID", CommandType.Text, new SqlParameter("@RevokedBy", revokedBy), new SqlParameter("@SessionID", sessionId));
            AuditLogger.Log(revokedBy, "SESSION_REVOKE", "Sessions", "Admin/Sessions.aspx", sessionId.ToString(), null, reason);
        }

        public static void TouchAnonymousCsrfCookie() => SecurityHelper.GetOrCreateCsrfToken();

        private static void ForceLogout()
        {
            FormsAuthentication.SignOut();
            ExpireCookie(GetSessionCookieName());
            ExpireCookie(ConfigurationManager.AppSettings["AuthCookieName"] ?? FormsAuthentication.FormsCookieName);
            if (HttpContext.Current != null) { HttpContext.Current.Session.Clear(); HttpContext.Current.Session.Abandon(); }
        }

        private static void ExpireCookie(string name)
        {
            if (HttpContext.Current == null) return;
            HttpContext.Current.Response.Cookies.Set(new HttpCookie(name, string.Empty) { Expires = DateTime.UtcNow.AddDays(-1), HttpOnly = true, Secure = HttpContext.Current.Request.IsSecureConnection || SecurityHelper.IsHttpsRequired(), SameSite = SameSiteMode.Lax });
        }

        private static int GetRememberMeDays() { int v; return int.TryParse(ConfigurationManager.AppSettings["RememberMeDays"], out v) ? v : 30; }
        private static int GetInactivityMinutes() { int v; return int.TryParse(ConfigurationManager.AppSettings["SessionInactivityMinutes"], out v) ? v : 1440; }
        private static string GetSessionCookieName() => ConfigurationManager.AppSettings["SessionCookieName"] ?? "WAMO_SESSION";
    }
}
