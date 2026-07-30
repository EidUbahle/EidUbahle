<%@ WebHandler Language="C#" Class="JournalsHandler" %>

using System;
using System.Web;
using System.Web.Script.Serialization;
using EidUbahle.CrossCutting;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Security;

/// <summary>
/// /Handlers/Journals.ashx – Journal Entry CRUD + post/reverse operations.
/// </summary>
public class JournalsHandler : IHttpHandler
{
    private static readonly JavaScriptSerializer _json =
        new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

    public bool IsReusable => false;

    public void ProcessRequest(HttpContext ctx)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.Cache.SetNoStore();

        if (!IsXhr(ctx.Request)) { WriteError(ctx, 400, "ERR_INVALID_REQUEST", "Invalid request"); return; }

        var claims = ctx.Items["JwtClaims"] as System.Collections.Generic.Dictionary<string, string>;
        if (claims == null) { WriteError(ctx, 401, "ERR_UNAUTHORIZED", "Authentication required"); return; }

        if (!Guid.TryParse(claims.GetValueOrDefault("tid"), out var tenantId) ||
            !Guid.TryParse(claims.GetValueOrDefault("sub"), out var userId))
        { WriteError(ctx, 401, "ERR_INVALID_TOKEN", "Invalid token claims"); return; }

        var method = ctx.Request.HttpMethod.ToUpper();
        var action = (ctx.Request.QueryString["action"] ?? "").ToLowerInvariant();

        try
        {
            switch (method)
            {
                case "GET":
                    HandleGet(ctx, tenantId, userId);
                    break;
                case "POST":
                    switch (action)
                    {
                        case "post":
                            RequirePerm(ctx, claims, "accounting.journal.post");
                            HandlePostAction(ctx, tenantId, userId);
                            break;
                        case "reverse":
                            RequirePerm(ctx, claims, "accounting.journal.reverse");
                            HandleReverseAction(ctx, tenantId, userId);
                            break;
                        default:
                            RequirePerm(ctx, claims, "accounting.journal.create");
                            HandleCreate(ctx, tenantId, userId);
                            break;
                    }
                    break;
                case "DELETE":
                    RequirePerm(ctx, claims, "accounting.journal.delete");
                    HandleDelete(ctx, tenantId, userId);
                    break;
                default:
                    WriteError(ctx, 405, "ERR_METHOD", "Method not allowed");
                    break;
            }
        }
        catch (Exception ex)
        {
            WriteError(ctx, 500, "ERR_SERVER",
                ConfigHelper.IsProduction ? "Internal server error" : ex.Message);
        }
    }

    private void HandleGet(HttpContext ctx, Guid tenantId, Guid userId)
    {
        var idStr = ctx.Request.QueryString["id"];
        if (!string.IsNullOrEmpty(idStr) && Guid.TryParse(idStr, out var jeId))
        {
            WriteResult(ctx, ServiceLocator.AccountingService.GetJournalEntry(jeId));
            return;
        }

        if (!Guid.TryParse(ctx.Request.QueryString["companyId"], out var companyId))
        { WriteError(ctx, 400, "ERR_BAD_REQUEST", "companyId is required"); return; }

        var search = ctx.Request.QueryString["search"];
        var status = ctx.Request.QueryString["status"];
        DateTime? startDate = null, endDate = null;
        if (DateTime.TryParse(ctx.Request.QueryString["startDate"], out var sd)) startDate = sd;
        if (DateTime.TryParse(ctx.Request.QueryString["endDate"],   out var ed)) endDate   = ed;
        int.TryParse(ctx.Request.QueryString["page"]     ?? "1",  out var page);
        int.TryParse(ctx.Request.QueryString["pageSize"] ?? "20", out var pageSize);

        WriteResult(ctx, ServiceLocator.AccountingService.GetJournalEntries(
            tenantId, companyId, search, status, startDate, endDate,
            Math.Max(1, page), Math.Clamp(pageSize, 1, 100)));
    }

    private void HandleCreate(HttpContext ctx, Guid tenantId, Guid userId)
    {
        var dto = _json.Deserialize<CreateJournalEntryDto>(ReadBody(ctx));
        if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Request body is empty"); return; }
        var result = ServiceLocator.AccountingService.CreateJournalEntry(tenantId, userId, dto);
        ctx.Response.StatusCode = result.Success ? 201 : 400;
        WriteResult(ctx, result);
    }

    private void HandlePostAction(HttpContext ctx, Guid tenantId, Guid userId)
    {
        var dto = _json.Deserialize<PostJournalEntryDto>(ReadBody(ctx));
        if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Request body is empty"); return; }
        WriteResult(ctx, ServiceLocator.AccountingService.PostJournalEntry(tenantId, userId, dto));
    }

    private void HandleReverseAction(HttpContext ctx, Guid tenantId, Guid userId)
    {
        var dto = _json.Deserialize<ReverseJournalEntryDto>(ReadBody(ctx));
        if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Request body is empty"); return; }
        WriteResult(ctx, ServiceLocator.AccountingService.ReverseJournalEntry(tenantId, userId, dto));
    }

    private void HandleDelete(HttpContext ctx, Guid tenantId, Guid userId)
    {
        if (!Guid.TryParse(ctx.Request.QueryString["id"], out var id))
        { WriteError(ctx, 400, "ERR_BAD_REQUEST", "id is required"); return; }
        WriteResult(ctx, ServiceLocator.AccountingService.DeleteJournalEntry(tenantId, userId, id));
    }

    private static void RequirePerm(HttpContext ctx,
        System.Collections.Generic.Dictionary<string, string> claims, string perm)
    {
        var isAdmin = claims.GetValueOrDefault("adm") == "True" || claims.GetValueOrDefault("sad") == "True";
        if (isAdmin) return;
        var perms = claims.GetValueOrDefault("perms") ?? "";
        if (!perms.Contains(perm))
        {
            WriteError(ctx, 403, "ERR_FORBIDDEN", "Insufficient permissions");
            ctx.Response.End();
        }
    }

    private static string ReadBody(HttpContext ctx)
    {
        using (var reader = new System.IO.StreamReader(ctx.Request.InputStream))
            return reader.ReadToEnd();
    }

    private static void WriteResult<T>(HttpContext ctx, T result)
        => ctx.Response.Write(_json.Serialize(result));

    private static void WriteError(HttpContext ctx, int status, string code, string msg)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.Write(_json.Serialize(new { success = false, errorCode = code, message = msg }));
    }

    private static bool IsXhr(HttpRequest req) =>
        string.Equals(req.Headers["X-Requested-With"], "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase)
        || req.ContentType?.Contains("application/json") == true;
}
