<%@ WebHandler Language="C#" Class="BanksHandler" %>

using System;
using System.Web;
using System.Web.Script.Serialization;
using EidUbahle.CrossCutting;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Security;

/// <summary>
/// /Handlers/Banks.ashx – Bank Account and Reconciliation management.
/// GET    ?companyId=             → list bank accounts
/// GET    ?id=                    → single bank account
/// GET    ?action=reconciliations&bankAccountId=  → reconciliation list
/// POST   ?action=create          → create bank account
/// POST   ?action=reconcile       → start reconciliation
/// PUT    (body)                  → update bank account
/// DELETE ?id=                    → delete bank account
/// </summary>
public class BanksHandler : IHttpHandler
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
                case "GET":    HandleGet(ctx, tenantId, userId, action); break;
                case "POST":   HandlePost(ctx, tenantId, userId, action, claims); break;
                case "PUT":    RequirePerm(ctx, claims, "banking.account.edit"); HandlePut(ctx, tenantId, userId); break;
                case "DELETE": RequirePerm(ctx, claims, "banking.account.delete"); HandleDelete(ctx, tenantId, userId); break;
                default: WriteError(ctx, 405, "ERR_METHOD", "Method not allowed"); break;
            }
        }
        catch (Exception ex)
        {
            WriteError(ctx, 500, "ERR_SERVER",
                ConfigHelper.IsProduction ? "Internal server error" : ex.Message);
        }
    }

    private void HandleGet(HttpContext ctx, Guid tenantId, Guid userId, string action)
    {
        switch (action)
        {
            case "reconciliations":
            {
                if (!Guid.TryParse(ctx.Request.QueryString["bankAccountId"], out var baId))
                { WriteError(ctx, 400, "ERR_BAD_REQUEST", "bankAccountId is required"); return; }
                WriteResult(ctx, ServiceLocator.AccountingService.GetReconciliations(baId));
                break;
            }
            case "currencies":
                WriteResult(ctx, ServiceLocator.AccountingService.GetCurrencies());
                break;
            case "rates":
            {
                var from = ctx.Request.QueryString["from"];
                WriteResult(ctx, ServiceLocator.AccountingService.GetExchangeRates(tenantId, from));
                break;
            }
            default:
            {
                var idStr = ctx.Request.QueryString["id"];
                if (!string.IsNullOrEmpty(idStr) && Guid.TryParse(idStr, out var bankId))
                {
                    WriteResult(ctx, ServiceLocator.AccountingService.GetBankAccount(bankId));
                    return;
                }
                if (!Guid.TryParse(ctx.Request.QueryString["companyId"], out var companyId))
                { WriteError(ctx, 400, "ERR_BAD_REQUEST", "companyId is required"); return; }
                bool activeOnly = ctx.Request.QueryString["activeOnly"] == "true";
                WriteResult(ctx, ServiceLocator.AccountingService.GetBankAccounts(tenantId, companyId, activeOnly));
                break;
            }
        }
    }

    private void HandlePost(HttpContext ctx, Guid tenantId, Guid userId, string action,
        System.Collections.Generic.Dictionary<string, string> claims)
    {
        var body = ReadBody(ctx);
        switch (action)
        {
            case "reconcile":
            {
                RequirePerm(ctx, claims, "banking.reconciliation.create");
                var dto = _json.Deserialize<CreateBankReconciliationDto>(body);
                if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Request body is empty"); return; }
                var result = ServiceLocator.AccountingService.CreateReconciliation(tenantId, userId, dto);
                ctx.Response.StatusCode = result.Success ? 201 : 400;
                WriteResult(ctx, result);
                break;
            }
            case "rate":
            {
                var dto = _json.Deserialize<CreateExchangeRateDto>(body);
                if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Request body is empty"); return; }
                var result = ServiceLocator.AccountingService.CreateExchangeRate(tenantId, userId, dto);
                ctx.Response.StatusCode = result.Success ? 201 : 400;
                WriteResult(ctx, result);
                break;
            }
            default:
            {
                RequirePerm(ctx, claims, "banking.account.create");
                var dto = _json.Deserialize<CreateBankAccountDto>(body);
                if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Request body is empty"); return; }
                var result = ServiceLocator.AccountingService.CreateBankAccount(tenantId, userId, dto);
                ctx.Response.StatusCode = result.Success ? 201 : 400;
                WriteResult(ctx, result);
                break;
            }
        }
    }

    private void HandlePut(HttpContext ctx, Guid tenantId, Guid userId)
    {
        var dto = _json.Deserialize<UpdateBankAccountDto>(ReadBody(ctx));
        if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Request body is empty"); return; }
        WriteResult(ctx, ServiceLocator.AccountingService.UpdateBankAccount(userId, dto));
    }

    private void HandleDelete(HttpContext ctx, Guid tenantId, Guid userId)
    {
        if (!Guid.TryParse(ctx.Request.QueryString["id"], out var id))
        { WriteError(ctx, 400, "ERR_BAD_REQUEST", "id is required"); return; }
        WriteResult(ctx, ServiceLocator.AccountingService.DeleteBankAccount(id, userId));
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
