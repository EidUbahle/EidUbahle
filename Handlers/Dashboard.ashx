<%@ WebHandler Language="C#" Class="DashboardHandler" %>

using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Script.Serialization;
using EidUbahle.CrossCutting;
using EidUbahle.Infrastructure.Security;

/// <summary>
/// /Handlers/Dashboard.ashx
/// Serves KPI data and recent transactions for the dashboard.
/// Returns live data when online; client caches in IndexedDB for offline view.
/// </summary>
public class DashboardHandler : IHttpHandler
{
    private static readonly JavaScriptSerializer _json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
    public bool IsReusable => false;

    public void ProcessRequest(HttpContext ctx)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.Cache.SetExpires(DateTime.UtcNow.AddSeconds(60));
        ctx.Response.Cache.SetCacheability(HttpCacheability.NoCache);

        var claims = ctx.Items["JwtClaims"] as Dictionary<string, string>;
        if (claims == null) { ctx.Response.StatusCode = 401; ctx.Response.Write("{\"success\":false}"); return; }

        try
        {
            Guid tenantId = Guid.TryParse(claims.GetValueOrDefault("tid"), out var tid) ? tid : Guid.Empty;
            Guid userId   = Guid.TryParse(claims.GetValueOrDefault("sub"), out var uid) ? uid : Guid.Empty;
            var period    = ctx.Request.QueryString["period"] ?? "month";
            var action    = ctx.Request.QueryString["action"] ?? "kpis";

            switch (action)
            {
                case "recent_transactions":
                    ServeRecentTransactions(ctx, tenantId, period);
                    break;
                default:
                    ServeKPIs(ctx, tenantId, period);
                    break;
            }
        }
        catch (Exception ex)
        {
            ctx.Response.StatusCode = 500;
            ctx.Response.Write(_json.Serialize(new { success = false, message = ConfigHelper.IsProduction ? "Server error" : ex.Message }));
        }
    }

    private void ServeKPIs(HttpContext ctx, Guid tenantId, string period)
    {
        // Phase 1 stub: returns placeholder KPIs.
        // Phase 3+ will query acc_JournalLines, sales_Invoices, etc.
        var kpis = new[]
        {
            new { label = "Total Revenue",   value = "$0.00",  icon = "cash-coin",       trend = (double?)null },
            new { label = "Total Expenses",  value = "$0.00",  icon = "credit-card",     trend = (double?)null },
            new { label = "Net Profit",      value = "$0.00",  icon = "graph-up-arrow",  trend = (double?)null },
            new { label = "Outstanding AR",  value = "$0.00",  icon = "people",          trend = (double?)null },
            new { label = "Outstanding AP",  value = "$0.00",  icon = "cart3",           trend = (double?)null },
            new { label = "Cash Balance",    value = "$0.00",  icon = "bank",            trend = (double?)null },
        };

        ctx.Response.Write(_json.Serialize(new { success = true, data = new { kpis, period } }));
    }

    private void ServeRecentTransactions(HttpContext ctx, Guid tenantId, string period)
    {
        // Phase 1 stub: expanded in Phase 3 with real journal/invoice data
        var transactions = new object[0];
        ctx.Response.Write(_json.Serialize(new { success = true, data = new { transactions } }));
    }
}
