using System;
using System.Data;
using System.Data.SqlClient;
using System.Web;

namespace WamoApp
{
    public static class AuditLogger
    {
        public static void Log(int? userId, string action, string module, string page, string recordId, string oldValue, string newValue)
        {
            try
            {
                DbHelper.ExecuteNonQuery(@"INSERT INTO AuditLogs (UserID, Action, Module, Page, RecordID, OldValue, NewValue, IPAddress, UserAgent, CreatedDate) VALUES (@UserID,@Action,@Module,@Page,@RecordID,@OldValue,@NewValue,@IPAddress,@UserAgent,GETUTCDATE())", CommandType.Text,
                    new SqlParameter("@UserID", (object)userId ?? DBNull.Value), new SqlParameter("@Action", action ?? string.Empty), new SqlParameter("@Module", module ?? string.Empty), new SqlParameter("@Page", page ?? string.Empty), new SqlParameter("@RecordID", (object)recordId ?? DBNull.Value), new SqlParameter("@OldValue", (object)oldValue ?? DBNull.Value), new SqlParameter("@NewValue", (object)newValue ?? DBNull.Value), new SqlParameter("@IPAddress", SecurityHelper.GetIpAddress()), new SqlParameter("@UserAgent", SecurityHelper.GetUserAgent()));
            }
            catch { }
        }

        public static void LogApplicationError(Exception exception, HttpContext context)
        {
            if (exception != null) Log(SessionManager.GetCurrentUserIdOrNull(), "ERROR", "Application", context != null ? context.Request.RawUrl : string.Empty, null, null, exception.Message);
        }
    }
}
