using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Web;

namespace WamoApp
{
    public static class PermissionManager
    {
        public static bool CanAccessPage(int userId, string pagePath)
        {
            if (SessionManager.IsInRole("Super Admin")) return true;
            var count = Convert.ToInt32(DbHelper.ExecuteScalar(@"SELECT COUNT(1) FROM UserRoles ur INNER JOIN RolePages rp ON rp.RoleID = ur.RoleID AND rp.IsAllowed = 1 INNER JOIN Pages p ON p.PageID = rp.PageID WHERE ur.UserID = @UserID AND ur.IsActive = 1 AND p.PagePath = @PagePath AND p.IsActive = 1", CommandType.Text, new SqlParameter("@UserID", userId), new SqlParameter("@PagePath", NormalizePagePath(pagePath))));
            return count > 0;
        }

        public static bool HasPermission(int userId, string moduleKey, string permissionName)
        {
            if (SessionManager.IsInRole("Super Admin")) return true;
            var count = Convert.ToInt32(DbHelper.ExecuteScalar(@"SELECT COUNT(1) FROM UserRoles ur INNER JOIN RolePermissions rp ON rp.RoleID = ur.RoleID AND rp.IsAllowed = 1 INNER JOIN Permissions p ON p.PermissionID = rp.PermissionID WHERE ur.UserID = @UserID AND ur.IsActive = 1 AND p.ModuleKey = @ModuleKey AND p.PermissionName = @PermissionName", CommandType.Text, new SqlParameter("@UserID", userId), new SqlParameter("@ModuleKey", moduleKey), new SqlParameter("@PermissionName", permissionName)));
            return count > 0;
        }

        public static void DemandPageAccess(string pagePath)
        {
            SessionManager.RequireAuthenticated();
            if (!CanAccessPage(SessionManager.GetCurrentUserId(), pagePath))
            {
                HttpContext.Current.Response.Redirect("~/AccessDenied.aspx", false);
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
        }

        public static void DemandPermission(string moduleKey, string permissionName)
        {
            SessionManager.RequireAuthenticated();
            if (!HasPermission(SessionManager.GetCurrentUserId(), moduleKey, permissionName)) throw new UnauthorizedAccessException("You do not have permission for this action.");
        }

        public static string BuildAdminMenuHtml()
        {
            if (!SessionManager.IsAuthenticated()) return string.Empty;
            var table = DbHelper.ExecuteDataTable(@"SELECT DISTINCT p.PageName, p.PagePath, p.MenuGroup, p.MenuOrder FROM Pages p INNER JOIN RolePages rp ON rp.PageID = p.PageID AND rp.IsAllowed = 1 INNER JOIN UserRoles ur ON ur.RoleID = rp.RoleID AND ur.UserID = @UserID AND ur.IsActive = 1 WHERE p.IsActive = 1 ORDER BY p.MenuGroup, p.MenuOrder, p.PageName", CommandType.Text, new SqlParameter("@UserID", SessionManager.GetCurrentUserId()));
            var sb = new StringBuilder();
            string current = null;
            foreach (DataRow row in table.Rows)
            {
                var group = row["MenuGroup"].ToString();
                if (!string.Equals(group, current, StringComparison.OrdinalIgnoreCase)) { sb.AppendFormat("<div class='list-group-item active small text-uppercase'>{0}</div>", SecurityHelper.HtmlEncode(group)); current = group; }
                sb.AppendFormat("<a class='list-group-item list-group-item-action' href='{0}'>{1}</a>", VirtualPathUtility.ToAbsolute("~/" + row["PagePath"]), SecurityHelper.HtmlEncode(row["PageName"].ToString()));
            }
            return sb.ToString();
        }

        public static string NormalizePagePath(string pagePath) => (pagePath ?? string.Empty).TrimStart('~', '/').Replace('\\', '/');
    }
}
