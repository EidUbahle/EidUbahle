<%@ WebHandler Language="C#" Class="SyncPullHandler" %>

using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Script.Serialization;
using EidUbahle.CrossCutting;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Security;

/// <summary>
/// /Handlers/SyncPull.ashx
/// Returns all records changed since the client's last sync timestamp.
/// Implements incremental sync using UpdatedAt > @Since.
/// </summary>
public class SyncPullHandler : IHttpHandler
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
            Guid tenantId = Guid.TryParse(claims.GetValueOrDefault("tid"), out var tid) ? tid : Guid.Empty;
            Guid userId   = Guid.TryParse(claims.GetValueOrDefault("sub"), out var uid) ? uid : Guid.Empty;

            DateTime since;
            if (!DateTime.TryParse(ctx.Request.QueryString["since"], out since))
                since = DateTime.MinValue;

            var deviceId = ctx.Request.QueryString["deviceId"] ?? "";
            int batchSize = 200;

            var records = new List<SyncRecordDto>();

            // ── Pull changes for each entity type ─────────────────────────────────
            // Phase 1: Pull Languages and Translations (always relevant)
            records.AddRange(PullTranslations(tenantId, since, batchSize));
            records.AddRange(PullLanguages(since, batchSize));

            // Phase 3+: Add more entity types as modules are built
            // records.AddRange(PullAccounts(tenantId, companyId, since, batchSize));
            // records.AddRange(PullInvoices(tenantId, companyId, branchId, since, batchSize));

            var response = new SyncPullResponseDto
            {
                Success         = true,
                ServerTimestamp = DateTime.UtcNow.ToString("o"),
                Records         = records,
                HasMore         = false,
            };

            ctx.Response.Write(_json.Serialize(response));
        }
        catch (Exception ex)
        {
            ctx.Response.StatusCode = 500;
            ctx.Response.Write(_json.Serialize(new { success = false, message = ConfigHelper.IsProduction ? "Server error" : ex.Message }));
        }
    }

    private List<SyncRecordDto> PullTranslations(Guid tenantId, DateTime since, int max)
    {
        var list = new List<SyncRecordDto>();
        try
        {
            using (var conn = new System.Data.SqlClient.SqlConnection(ConfigHelper.DbConnectionString))
            {
                conn.Open();
                const string sql = @"
                    SELECT TOP (@Max) Id, LanguageCode, TranslationKey, [Text], Module, IsCustom, IsDeleted, UpdatedAt
                    FROM sys_Translations
                    WHERE UpdatedAt > @Since
                      AND (TenantId IS NULL OR TenantId = @TenantId)
                    ORDER BY UpdatedAt ASC";
                using (var cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Max", max);
                    cmd.Parameters.AddWithValue("@Since", since);
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var id = r.GetGuid(0);
                            var payload = _json.Serialize(new {
                                id = id, languageCode = r.GetString(1), key = r.GetString(2),
                                value = r.GetString(3), module = r.GetString(4), isCustom = r.GetBoolean(5)
                            });
                            list.Add(new SyncRecordDto {
                                EntityType = "Translation", EntityId = id,
                                Operation  = r.GetBoolean(6) ? "delete" : "update",
                                PayloadJson = payload,
                                ClientTimestamp = r.GetDateTime(7).ToString("o")
                            });
                        }
                    }
                }
            }
        }
        catch { }
        return list;
    }

    private List<SyncRecordDto> PullLanguages(DateTime since, int max)
    {
        var list = new List<SyncRecordDto>();
        try
        {
            using (var conn = new System.Data.SqlClient.SqlConnection(ConfigHelper.DbConnectionString))
            {
                conn.Open();
                const string sql = @"SELECT TOP (@Max) Id, Code, Name, NativeName, Direction, FlagIcon, IsDefault, IsActive, SortOrder, CreatedAt FROM sys_Languages WHERE CreatedAt > @Since ORDER BY CreatedAt";
                using (var cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Max", max);
                    cmd.Parameters.AddWithValue("@Since", since);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var id = r.GetGuid(0);
                            var payload = _json.Serialize(new {
                                id, code = r.GetString(1), name = r.GetString(2),
                                nativeName = r.IsDBNull(3)?null:r.GetString(3),
                                direction = r.IsDBNull(4)?"ltr":r.GetString(4),
                                flagIcon = r.IsDBNull(5)?null:r.GetString(5),
                                isDefault = r.GetBoolean(6), isActive = r.GetBoolean(7), sortOrder = r.GetInt32(8)
                            });
                            list.Add(new SyncRecordDto {
                                EntityType = "Language", EntityId = id, Operation = "update",
                                PayloadJson = payload, ClientTimestamp = r.GetDateTime(9).ToString("o")
                            });
                        }
                    }
                }
            }
        }
        catch { }
        return list;
    }
}
