<%@ WebHandler Language="C#" Class="UserPreferenceHandler" %>

using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Script.Serialization;
using EidUbahle.CrossCutting;
using EidUbahle.Infrastructure.Security;

/// <summary>
/// /Handlers/UserPreference.ashx
/// Persists user UI preferences (theme, layout, language, active branch) to the server.
/// Best-effort: client stores locally first, this syncs to the cloud.
/// </summary>
public class UserPreferenceHandler : IHttpHandler
{
    private static readonly JavaScriptSerializer _json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
    public bool IsReusable => false;

    public void ProcessRequest(HttpContext ctx)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.Cache.SetNoStore();

        var claims = ctx.Items["JwtClaims"] as Dictionary<string, string>;
        if (claims == null) { ctx.Response.StatusCode = 401; ctx.Response.Write("{\"success\":false}"); return; }

        try
        {
            Guid userId   = Guid.TryParse(claims.GetValueOrDefault("sub"), out var uid) ? uid : Guid.Empty;
            Guid tenantId = Guid.TryParse(claims.GetValueOrDefault("tid"), out var tid) ? tid : Guid.Empty;

            string body;
            using (var r = new StreamReader(ctx.Request.InputStream)) body = r.ReadToEnd();
            var req = _json.Deserialize<Dictionary<string, string>>(body);
            if (req == null) { ctx.Response.StatusCode = 400; ctx.Response.Write("{\"success\":false}"); return; }

            var key   = req.GetValueOrDefault("key");
            var value = req.GetValueOrDefault("value");

            if (string.IsNullOrEmpty(key))
            { ctx.Response.StatusCode = 400; ctx.Response.Write("{\"success\":false,\"message\":\"Key required\"}"); return; }

            // Validate allowed preference keys
            var allowed = new HashSet<string> { "languageCode", "themeMode", "accentColor", "activeLayout", "activeBranchId" };
            if (!allowed.Contains(key))
            { ctx.Response.StatusCode = 400; ctx.Response.Write("{\"success\":false,\"message\":\"Invalid preference key\"}"); return; }

            SavePreference(userId, key, value);
            ctx.Response.Write(_json.Serialize(new { success = true }));
        }
        catch (Exception ex)
        {
            ctx.Response.StatusCode = 500;
            ctx.Response.Write(_json.Serialize(new { success = false, message = ConfigHelper.IsProduction ? "Server error" : ex.Message }));
        }
    }

    private void SavePreference(Guid userId, string key, string value)
    {
        using (var conn = new System.Data.SqlClient.SqlConnection(ConfigHelper.DbConnectionString))
        {
            conn.Open();
            // Map preference key to sys_Users column
            string column = null;
            switch (key)
            {
                case "languageCode":  column = "LanguageCode";  break;
                case "themeMode":     column = "ThemeMode";     break;
                case "accentColor":   column = "AccentColor";   break;
                case "activeLayout":  column = "ActiveLayout";  break;
                default: break;
            }

            if (column != null)
            {
                // Prevent SQL injection: column name is whitelist-validated above
                using (var cmd = new System.Data.SqlClient.SqlCommand(
                    $"UPDATE sys_Users SET [{column}]=@Val, UpdatedAt=GETUTCDATE() WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Val", (object)value ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Id", userId);
                    cmd.ExecuteNonQuery();
                }
            }
            else if (key == "activeBranchId")
            {
                // Store active branch in settings table (user-level)
                const string sql = @"
                    IF EXISTS (SELECT 1 FROM sys_Settings WHERE UserId=@UserId AND SettingKey='activeBranchId')
                        UPDATE sys_Settings SET SettingValue=@Val, UpdatedAt=GETUTCDATE() WHERE UserId=@UserId AND SettingKey='activeBranchId'
                    ELSE
                        INSERT INTO sys_Settings(Id,UserId,SettingKey,SettingValue,DataType,UpdatedAt) VALUES(NEWID(),@UserId,'activeBranchId',@Val,'string',GETUTCDATE())";
                using (var cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Val", (object)value ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }

            // Invalidate cached permissions
            ServiceLocator.Cache.Remove($"perms:{userId}");
        }
    }
}
