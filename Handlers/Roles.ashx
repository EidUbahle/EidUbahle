<%@ WebHandler Language="C#" Class="RolesHandler" %>

using System;
using System.Web;
using System.Web.Script.Serialization;
using EidUbahle.CrossCutting;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Security;

/// <summary>
/// /Handlers/Roles.ashx – Role and Permission management.
/// All actions require IsTenantAdmin or IsSuperAdmin.
/// </summary>
public class RolesHandler : IHttpHandler
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

        var isAdmin = claims.GetValueOrDefault("adm") == "True" ||
                      claims.GetValueOrDefault("sad") == "True";
        if (!isAdmin) { WriteError(ctx, 403, "ERR_FORBIDDEN", "Admin access required"); return; }

        var method = ctx.Request.HttpMethod.ToUpper();
        var action = (ctx.Request.QueryString["action"] ?? "").ToLowerInvariant();

        try
        {
            switch (method)
            {
                case "GET":  HandleGet(ctx, tenantId, action);  break;
                case "POST": HandlePost(ctx, tenantId, action); break;
                case "PUT":  HandlePut(ctx, tenantId);          break;
                case "DELETE": HandleDelete(ctx, tenantId);     break;
                default: WriteError(ctx, 405, "ERR_METHOD", "Method not allowed"); break;
            }
        }
        catch (Exception ex)
        {
            WriteError(ctx, 500, "ERR_SERVER",
                ConfigHelper.IsProduction ? "Internal server error" : ex.Message);
        }
    }

    private void HandleGet(HttpContext ctx, Guid tenantId, string action)
    {
        switch (action)
        {
            case "permissions":
            {
                var result = ServiceLocator.RoleService.GetAllPermissions();
                WriteResult(ctx, result);
                break;
            }
            case "permission_matrix":
            {
                var result = ServiceLocator.RoleService.GetPermissionMatrix();
                WriteResult(ctx, result);
                break;
            }
            default:
            {
                var idStr = ctx.Request.QueryString["id"];
                if (!string.IsNullOrEmpty(idStr) && Guid.TryParse(idStr, out var roleId))
                {
                    var result = ServiceLocator.RoleService.GetById(tenantId, roleId);
                    WriteResult(ctx, result);
                }
                else
                {
                    var search = ctx.Request.QueryString["search"];
                    int.TryParse(ctx.Request.QueryString["page"] ?? "1", out var page);
                    int.TryParse(ctx.Request.QueryString["pageSize"] ?? "50", out var pageSize);
                    var result = ServiceLocator.RoleService.GetRoles(tenantId, search,
                        Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
                    WriteResult(ctx, result);
                }
                break;
            }
        }
    }

    private void HandlePost(HttpContext ctx, Guid tenantId, string action)
    {
        var body = ReadBody(ctx);
        var dto = _json.Deserialize<CreateRoleDto>(body);
        if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Request body is empty"); return; }
        var result = ServiceLocator.RoleService.CreateRole(tenantId, dto);
        ctx.Response.StatusCode = result.Success ? 201 : 400;
        WriteResult(ctx, result);
    }

    private void HandlePut(HttpContext ctx, Guid tenantId)
    {
        var body = ReadBody(ctx);
        var dto = _json.Deserialize<UpdateRoleDto>(body);
        if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Request body is empty"); return; }
        var result = ServiceLocator.RoleService.UpdateRole(tenantId, dto);
        WriteResult(ctx, result);
    }

    private void HandleDelete(HttpContext ctx, Guid tenantId)
    {
        var idStr = ctx.Request.QueryString["id"];
        if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var roleId))
        { WriteError(ctx, 400, "ERR_BAD_REQUEST", "id parameter is required"); return; }
        var result = ServiceLocator.RoleService.DeleteRole(tenantId, roleId);
        WriteResult(ctx, result);
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
