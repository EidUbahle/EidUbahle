<%@ WebHandler Language="C#" Class="FiscalPeriodsHandler" %>

using System;
using System.Web;
using System.Web.Script.Serialization;
using EidUbahle.CrossCutting;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Security;

/// <summary>
/// /Handlers/FiscalPeriods.ashx – Fiscal Year and Period management.
/// GET    ?companyId=           → list fiscal years
/// GET    ?fiscalYearId=        → single fiscal year with periods
/// POST   ?action=create        → create fiscal year
/// POST   ?action=close_period  → close a period
/// POST   ?action=close_year    → close entire fiscal year
/// </summary>
public class FiscalPeriodsHandler : IHttpHandler
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
                    RequirePerm(ctx, claims, "accounting.period.create");
                    HandlePost(ctx, tenantId, userId, action);
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
        var fyStr = ctx.Request.QueryString["fiscalYearId"];
        if (!string.IsNullOrEmpty(fyStr) && Guid.TryParse(fyStr, out var fyId))
        {
            WriteResult(ctx, ServiceLocator.AccountingService.GetFiscalYear(fyId));
            return;
        }

        if (!Guid.TryParse(ctx.Request.QueryString["companyId"], out var companyId))
        { WriteError(ctx, 400, "ERR_BAD_REQUEST", "companyId or fiscalYearId is required"); return; }

        WriteResult(ctx, ServiceLocator.AccountingService.GetFiscalYears(tenantId, companyId));
    }

    private void HandlePost(HttpContext ctx, Guid tenantId, Guid userId, string action)
    {
        var body = ReadBody(ctx);
        switch (action)
        {
            case "create":
            {
                var dto = _json.Deserialize<CreateFiscalYearDto>(body);
                if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Request body is empty"); return; }
                var result = ServiceLocator.AccountingService.CreateFiscalYear(tenantId, userId, dto);
                ctx.Response.StatusCode = result.Success ? 201 : 400;
                WriteResult(ctx, result);
                break;
            }
            case "close_period":
            {
                RequirePerm(ctx, ctx.Items["JwtClaims"] as System.Collections.Generic.Dictionary<string, string>, "accounting.period.close");
                var dto = _json.Deserialize<CloseFiscalPeriodDto>(body);
                if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Request body is empty"); return; }
                WriteResult(ctx, ServiceLocator.AccountingService.CloseFiscalPeriod(userId, dto));
                break;
            }
            case "close_year":
            {
                RequirePerm(ctx, ctx.Items["JwtClaims"] as System.Collections.Generic.Dictionary<string, string>, "accounting.period.close");
                var dto = _json.Deserialize<CloseFiscalYearDto>(body);
                if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Request body is empty"); return; }
                WriteResult(ctx, ServiceLocator.AccountingService.CloseFiscalYear(userId, dto));
                break;
            }
            default:
                WriteError(ctx, 400, "ERR_UNKNOWN_ACTION", "Unknown action");
                break;
        }
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
