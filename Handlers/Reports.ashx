<%@ WebHandler Language="C#" Class="ReportsHandler" %>

using System;
using System.Web;
using System.Web.Script.Serialization;
using EidUbahle.CrossCutting;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Security;

/// <summary>
/// /Handlers/Reports.ashx – Financial report generation.
/// GET ?report=trialbalance|balancesheet|incomestatement &companyId=&fiscalYearId=&startPeriod=&endPeriod=
/// </summary>
public class ReportsHandler : IHttpHandler
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

        if (ctx.Request.HttpMethod.ToUpper() != "GET")
        { WriteError(ctx, 405, "ERR_METHOD", "Method not allowed"); return; }

        try
        {
            var report = (ctx.Request.QueryString["report"] ?? "").ToLowerInvariant();

            if (!Guid.TryParse(ctx.Request.QueryString["companyId"], out var companyId))
            { WriteError(ctx, 400, "ERR_BAD_REQUEST", "companyId is required"); return; }

            Guid? fiscalYearId = null;
            if (Guid.TryParse(ctx.Request.QueryString["fiscalYearId"], out var fyId)) fiscalYearId = fyId;

            int startPeriod = 1, endPeriod = 12;
            if (int.TryParse(ctx.Request.QueryString["startPeriod"], out var sp)) startPeriod = sp;
            if (int.TryParse(ctx.Request.QueryString["endPeriod"],   out var ep)) endPeriod   = ep;
            bool includeZero = ctx.Request.QueryString["includeZero"] == "true";

            var filter = new ReportFilterDto
            {
                CompanyId = companyId,
                FiscalYearId = fiscalYearId,
                StartPeriod = startPeriod,
                EndPeriod = endPeriod,
                IncludeZeroBalances = includeZero
            };

            switch (report)
            {
                case "trialbalance":
                    RequirePerm(ctx, claims, "reports.trialbalance.view");
                    WriteResult(ctx, ServiceLocator.AccountingService.GetTrialBalance(tenantId, filter));
                    break;
                case "balancesheet":
                    RequirePerm(ctx, claims, "reports.balancesheet.view");
                    WriteResult(ctx, ServiceLocator.AccountingService.GetBalanceSheet(tenantId, filter));
                    break;
                case "incomestatement":
                    RequirePerm(ctx, claims, "reports.incomestatement.view");
                    WriteResult(ctx, ServiceLocator.AccountingService.GetIncomeStatement(tenantId, filter));
                    break;
                default:
                    WriteError(ctx, 400, "ERR_UNKNOWN_REPORT",
                        "Unknown report. Valid values: trialbalance, balancesheet, incomestatement");
                    break;
            }
        }
        catch (Exception ex)
        {
            WriteError(ctx, 500, "ERR_SERVER",
                ConfigHelper.IsProduction ? "Internal server error" : ex.Message);
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
