<%@ WebHandler Language="C#" Class="SearchHandler" %>

using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Script.Serialization;
using EidUbahle.CrossCutting;

/// <summary>
/// /Handlers/Search.ashx
/// Global full-text search across Invoices, Customers, Accounts, Payments, Products.
/// Phase 1 stub: returns empty results.
/// Phase 5+ will implement real search across all modules with proper indexing.
/// </summary>
public class SearchHandler : IHttpHandler
{
    private static readonly JavaScriptSerializer _json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
    public bool IsReusable => false;

    public void ProcessRequest(HttpContext ctx)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.Cache.SetCacheability(HttpCacheability.NoCache);

        var claims = ctx.Items["JwtClaims"] as Dictionary<string, string>;
        if (claims == null) { ctx.Response.StatusCode = 401; ctx.Response.Write("{\"success\":false}"); return; }

        try
        {
            var q = (ctx.Request.QueryString["q"] ?? "").Trim();

            if (string.IsNullOrEmpty(q) || q.Length < 2)
            {
                ctx.Response.Write(_json.Serialize(new { success = true, data = new { results = new object[0], total = 0, query = q } }));
                return;
            }

            // Phase 1 stub: returns empty results with category placeholders.
            // Phase 5+ will search: sales_Invoices, crm_Contacts, acc_ChartOfAccounts,
            // acc_JournalHeaders, sales_Payments, inv_Products via full-text index.
            var results = new object[0];

            ctx.Response.Write(_json.Serialize(new
            {
                success = true,
                data = new
                {
                    results,
                    total = 0,
                    query = q,
                    categories = new[]
                    {
                        new { key = "invoices",  label = "Invoices",  count = 0 },
                        new { key = "customers", label = "Customers", count = 0 },
                        new { key = "accounts",  label = "Accounts",  count = 0 },
                        new { key = "products",  label = "Products",  count = 0 },
                    }
                }
            }));
        }
        catch (Exception ex)
        {
            ctx.Response.StatusCode = 500;
            ctx.Response.Write(_json.Serialize(new { success = false, message = ConfigHelper.IsProduction ? "Server error" : ex.Message }));
        }
    }
}
