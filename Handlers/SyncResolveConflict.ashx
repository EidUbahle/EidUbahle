<%@ WebHandler Language="C#" Class="SyncResolveConflictHandler" %>

using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Script.Serialization;
using EidUbahle.CrossCutting;
using EidUbahle.Domain.Enums;

/// <summary>
/// /Handlers/SyncResolveConflict.ashx
/// Allows authorized users (tenant admin or data-owner) to resolve sync conflicts
/// for entities that require Manual Merge (accounting journals, inventory counts).
///
/// Phase 1: Records resolution decision in sys_SyncConflicts.
/// Phase 3+: Will apply the merge to the actual entity tables.
///
/// POST body (JSON):
/// {
///   "conflictId": "<guid>",
///   "resolution": "client_wins" | "server_wins" | "merged",
///   "mergedData": { ... }   // Only when resolution == "merged"
/// }
/// </summary>
public class SyncResolveConflictHandler : IHttpHandler
{
    private static readonly JavaScriptSerializer _json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
    public bool IsReusable => false;

    public void ProcessRequest(HttpContext ctx)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.Cache.SetCacheability(HttpCacheability.NoCache);

        var claims = ctx.Items["JwtClaims"] as Dictionary<string, string>;
        if (claims == null) { ctx.Response.StatusCode = 401; ctx.Response.Write("{\"success\":false}"); return; }

        if (!string.Equals(ctx.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Response.StatusCode = 405;
            ctx.Response.Write(_json.Serialize(new { success = false, message = "Method not allowed" }));
            return;
        }

        try
        {
            // Read POST body
            string body;
            using (var reader = new StreamReader(ctx.Request.InputStream))
                body = reader.ReadToEnd();

            var payload = _json.Deserialize<Dictionary<string, object>>(body);
            if (payload == null)
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.Write(_json.Serialize(new { success = false, message = "Invalid request body" }));
                return;
            }

            var conflictIdStr  = payload.ContainsKey("conflictId")  ? payload["conflictId"].ToString()  : "";
            var resolution     = payload.ContainsKey("resolution")   ? payload["resolution"].ToString()  : "";
            var mergedDataRaw  = payload.ContainsKey("mergedData")   ? payload["mergedData"]              : null;

            if (!Guid.TryParse(conflictIdStr, out Guid conflictId))
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.Write(_json.Serialize(new { success = false, message = "Invalid conflictId" }));
                return;
            }

            var validResolutions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "client_wins", "server_wins", "merged" };
            if (!validResolutions.Contains(resolution))
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.Write(_json.Serialize(new { success = false, message = "Invalid resolution. Use: client_wins, server_wins, merged" }));
                return;
            }

            if (string.Equals(resolution, "merged", StringComparison.OrdinalIgnoreCase) && mergedDataRaw == null)
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.Write(_json.Serialize(new { success = false, message = "mergedData is required when resolution is 'merged'" }));
                return;
            }

            Guid resolvedBy = Guid.TryParse(claims.GetValueOrDefault("sub"), out var uid) ? uid : Guid.Empty;

            // Phase 1: record resolution in sys_SyncConflicts
            // Phase 3+: apply merged data to the target entity table
            RecordResolution(conflictId, resolution, mergedDataRaw, resolvedBy);

            ctx.Response.Write(_json.Serialize(new
            {
                success = true,
                data = new { conflictId, resolution, resolvedAt = DateTime.UtcNow, message = "Conflict resolved successfully" }
            }));
        }
        catch (Exception ex)
        {
            ctx.Response.StatusCode = 500;
            ctx.Response.Write(_json.Serialize(new { success = false, message = ConfigHelper.IsProduction ? "Server error" : ex.Message }));
        }
    }

    private void RecordResolution(Guid conflictId, string resolution, object mergedData, Guid resolvedBy)
    {
        // Phase 1 stub — logs the resolution decision.
        // Phase 3+ will open a UoW, load the conflict record, apply mergedData to target table,
        // mark conflict as Resolved, and write an audit log entry.

        string connStr = ConfigHelper.ConnectionString;
        using (var conn = new System.Data.SqlClient.SqlConnection(connStr))
        {
            conn.Open();
            string mergedJson = mergedData != null
                ? new JavaScriptSerializer().Serialize(mergedData)
                : null;

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE sys_SyncConflicts
                    SET    Status       = 'Resolved',
                           Resolution  = @Resolution,
                           ResolvedBy  = @ResolvedBy,
                           ResolvedAt  = GETUTCDATE(),
                           MergedData  = @MergedData,
                           UpdatedAt   = GETUTCDATE()
                    WHERE  Id = @Id";
                cmd.Parameters.AddWithValue("@Id",         conflictId);
                cmd.Parameters.AddWithValue("@Resolution", resolution);
                cmd.Parameters.AddWithValue("@ResolvedBy", (object)resolvedBy == null ? DBNull.Value : (object)resolvedBy);
                cmd.Parameters.AddWithValue("@MergedData", (object)mergedJson ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
