<%@ WebHandler Language="C#" Class="TenantHandler" %>

using System;
using System.Web;
using System.Web.Script.Serialization;
using EidUbahle.CrossCutting;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Security;

/// <summary>
/// /Handlers/Tenant.ashx – Tenant settings, Company and Branch management,
/// and the onboarding wizard flow.
/// </summary>
public class TenantHandler : IHttpHandler
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

        if (!Guid.TryParse(claims.GetValueOrDefault("tid"), out var tenantId))
        { WriteError(ctx, 401, "ERR_INVALID_TOKEN", "Invalid token claims"); return; }

        var resource = (ctx.Request.QueryString["resource"] ?? "").ToLowerInvariant();
        var method = ctx.Request.HttpMethod.ToUpper();
        var action = (ctx.Request.QueryString["action"] ?? "").ToLowerInvariant();

        try
        {
            switch (resource)
            {
                case "settings":   HandleSettings(ctx, tenantId, method, claims);   break;
                case "company":    HandleCompany(ctx, tenantId, method, claims);    break;
                case "branch":     HandleBranch(ctx, tenantId, method, claims);     break;
                case "onboarding": HandleOnboarding(ctx, tenantId, method, action); break;
                default:
                    WriteError(ctx, 400, "ERR_UNKNOWN_RESOURCE",
                        "Unknown resource. Use: settings, company, branch, onboarding");
                    break;
            }
        }
        catch (Exception ex)
        {
            WriteError(ctx, 500, "ERR_SERVER",
                ConfigHelper.IsProduction ? "Internal server error" : ex.Message);
        }
    }

    // ── Settings ─────────────────────────────────────────────────────────

    private void HandleSettings(HttpContext ctx, Guid tenantId, string method,
        System.Collections.Generic.Dictionary<string, string> claims)
    {
        switch (method)
        {
            case "GET":
                WriteResult(ctx, ServiceLocator.TenantService.GetSettings(tenantId));
                break;
            case "PUT":
                RequireAdmin(ctx, claims);
                var dto = _json.Deserialize<UpdateTenantSettingsDto>(ReadBody(ctx));
                if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Body required"); return; }
                WriteResult(ctx, ServiceLocator.TenantService.UpdateSettings(tenantId, dto));
                break;
            default:
                WriteError(ctx, 405, "ERR_METHOD", "Method not allowed"); break;
        }
    }

    // ── Company ───────────────────────────────────────────────────────────

    private void HandleCompany(HttpContext ctx, Guid tenantId, string method,
        System.Collections.Generic.Dictionary<string, string> claims)
    {
        RequireAdmin(ctx, claims);
        switch (method)
        {
            case "GET":
            {
                var idStr = ctx.Request.QueryString["id"];
                if (!string.IsNullOrEmpty(idStr) && Guid.TryParse(idStr, out var id))
                    WriteResult(ctx, ServiceLocator.TenantService.GetCompanyById(tenantId, id));
                else
                {
                    var incInactive = ctx.Request.QueryString["includeInactive"] == "true";
                    WriteResult(ctx, ServiceLocator.TenantService.GetCompanies(tenantId, incInactive));
                }
                break;
            }
            case "POST":
            {
                var dto = _json.Deserialize<CreateCompanyDto>(ReadBody(ctx));
                if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Body required"); return; }
                var r = ServiceLocator.TenantService.CreateCompany(tenantId, dto);
                ctx.Response.StatusCode = r.Success ? 201 : 400;
                WriteResult(ctx, r);
                break;
            }
            case "PUT":
            {
                var dto = _json.Deserialize<UpdateCompanyDto>(ReadBody(ctx));
                if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Body required"); return; }
                WriteResult(ctx, ServiceLocator.TenantService.UpdateCompany(tenantId, dto));
                break;
            }
            case "DELETE":
            {
                var idStr = ctx.Request.QueryString["id"];
                if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var id))
                { WriteError(ctx, 400, "ERR_BAD_REQUEST", "id required"); return; }
                WriteResult(ctx, ServiceLocator.TenantService.DeleteCompany(tenantId, id));
                break;
            }
            default:
                WriteError(ctx, 405, "ERR_METHOD", "Method not allowed"); break;
        }
    }

    // ── Branch ────────────────────────────────────────────────────────────

    private void HandleBranch(HttpContext ctx, Guid tenantId, string method,
        System.Collections.Generic.Dictionary<string, string> claims)
    {
        RequireAdmin(ctx, claims);
        switch (method)
        {
            case "GET":
            {
                var idStr = ctx.Request.QueryString["id"];
                if (!string.IsNullOrEmpty(idStr) && Guid.TryParse(idStr, out var id))
                    WriteResult(ctx, ServiceLocator.TenantService.GetBranchById(tenantId, id));
                else
                {
                    Guid? companyId = null;
                    var cidStr = ctx.Request.QueryString["companyId"];
                    if (!string.IsNullOrEmpty(cidStr) && Guid.TryParse(cidStr, out var cid)) companyId = cid;
                    var incInactive = ctx.Request.QueryString["includeInactive"] == "true";
                    WriteResult(ctx, ServiceLocator.TenantService.GetBranches(tenantId, companyId, incInactive));
                }
                break;
            }
            case "POST":
            {
                var dto = _json.Deserialize<CreateBranchDto>(ReadBody(ctx));
                if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Body required"); return; }
                var r = ServiceLocator.TenantService.CreateBranch(tenantId, dto);
                ctx.Response.StatusCode = r.Success ? 201 : 400;
                WriteResult(ctx, r);
                break;
            }
            case "PUT":
            {
                var dto = _json.Deserialize<UpdateBranchDto>(ReadBody(ctx));
                if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Body required"); return; }
                WriteResult(ctx, ServiceLocator.TenantService.UpdateBranch(tenantId, dto));
                break;
            }
            case "DELETE":
            {
                var idStr = ctx.Request.QueryString["id"];
                if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var id))
                { WriteError(ctx, 400, "ERR_BAD_REQUEST", "id required"); return; }
                WriteResult(ctx, ServiceLocator.TenantService.DeleteBranch(tenantId, id));
                break;
            }
            default:
                WriteError(ctx, 405, "ERR_METHOD", "Method not allowed"); break;
        }
    }

    // ── Onboarding ────────────────────────────────────────────────────────

    private void HandleOnboarding(HttpContext ctx, Guid tenantId, string method, string action)
    {
        switch (method)
        {
            case "GET":
                WriteResult(ctx, ServiceLocator.TenantService.GetOnboardingStatus(tenantId));
                break;
            case "POST":
            {
                var body = ReadBody(ctx);
                switch (action)
                {
                    case "step1":
                    {
                        var dto = _json.Deserialize<OnboardingStep1Dto>(body);
                        if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Body required"); return; }
                        WriteResult(ctx, ServiceLocator.TenantService.OnboardingStep1(tenantId, dto));
                        break;
                    }
                    case "step2":
                    {
                        var dto = _json.Deserialize<OnboardingStep2Dto>(body);
                        if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Body required"); return; }
                        WriteResult(ctx, ServiceLocator.TenantService.OnboardingStep2(tenantId, dto));
                        break;
                    }
                    case "step3":
                    {
                        var dto = _json.Deserialize<OnboardingStep3Dto>(body);
                        if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Body required"); return; }
                        WriteResult(ctx, ServiceLocator.TenantService.OnboardingStep3(tenantId, dto));
                        break;
                    }
                    default:
                        WriteError(ctx, 400, "ERR_UNKNOWN_ACTION",
                            "Unknown onboarding action. Use step1, step2, step3");
                        break;
                }
                break;
            }
            default:
                WriteError(ctx, 405, "ERR_METHOD", "Method not allowed"); break;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void RequireAdmin(HttpContext ctx,
        System.Collections.Generic.Dictionary<string, string> claims)
    {
        var isAdmin = claims.GetValueOrDefault("adm") == "True" ||
                      claims.GetValueOrDefault("sad") == "True";
        if (!isAdmin)
        {
            WriteError(ctx, 403, "ERR_FORBIDDEN", "Admin access required");
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
