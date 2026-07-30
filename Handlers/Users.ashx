<%@ WebHandler Language="C#" Class="UsersHandler" %>

using System;
using System.Web;
using System.Web.Script.Serialization;
using EidUbahle.CrossCutting;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Security;

/// <summary>
/// /Handlers/Users.ashx – User management CRUD + invite/password operations.
/// All actions require a valid JWT. Admin actions require IsTenantAdmin=true.
/// </summary>
public class UsersHandler : IHttpHandler
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
            !Guid.TryParse(claims.GetValueOrDefault("sub"), out var requestingUserId))
        { WriteError(ctx, 401, "ERR_INVALID_TOKEN", "Invalid token claims"); return; }

        var method = ctx.Request.HttpMethod.ToUpper();
        var action = (ctx.Request.QueryString["action"] ?? "").ToLowerInvariant();

        try
        {
            switch (method)
            {
                case "GET":
                    HandleGet(ctx, claims, tenantId, action);
                    break;
                case "POST":
                    RequireAdmin(ctx, claims);
                    HandlePost(ctx, tenantId, requestingUserId, action);
                    break;
                case "PUT":
                    RequireAdmin(ctx, claims);
                    HandlePut(ctx, tenantId, requestingUserId, action);
                    break;
                case "DELETE":
                    RequireAdmin(ctx, claims);
                    HandleDelete(ctx, tenantId, requestingUserId, action);
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

    // ── GET ──────────────────────────────────────────────────────────────

    private void HandleGet(HttpContext ctx,
        System.Collections.Generic.Dictionary<string, string> claims,
        Guid tenantId, string action)
    {
        switch (action)
        {
            case "me":
            {
                if (!Guid.TryParse(claims.GetValueOrDefault("sub"), out var meId))
                { WriteError(ctx, 401, "ERR_INVALID_TOKEN", "Invalid token"); return; }
                var result = ServiceLocator.UserService.GetById(tenantId, meId);
                WriteResult(ctx, result);
                break;
            }
            case "invitations":
            {
                RequireAdminGet(ctx, claims);
                var result = ServiceLocator.UserService.GetInvitations(tenantId);
                WriteResult(ctx, result);
                break;
            }
            default:
            {
                RequireAdminGet(ctx, claims);
                var idStr = ctx.Request.QueryString["id"];
                if (!string.IsNullOrEmpty(idStr) && Guid.TryParse(idStr, out var userId))
                {
                    var result = ServiceLocator.UserService.GetById(tenantId, userId);
                    WriteResult(ctx, result);
                }
                else
                {
                    var search = ctx.Request.QueryString["search"];
                    bool? isActive = null;
                    var activeStr = ctx.Request.QueryString["isActive"];
                    if (!string.IsNullOrEmpty(activeStr)) isActive = activeStr == "true";
                    int.TryParse(ctx.Request.QueryString["page"] ?? "1", out var page);
                    int.TryParse(ctx.Request.QueryString["pageSize"] ?? "20", out var pageSize);
                    var result = ServiceLocator.UserService.GetUsers(tenantId, search, isActive,
                        Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
                    WriteResult(ctx, result);
                }
                break;
            }
        }
    }

    // ── POST ─────────────────────────────────────────────────────────────

    private void HandlePost(HttpContext ctx, Guid tenantId, Guid requestingUserId, string action)
    {
        var body = ReadBody(ctx);
        switch (action)
        {
            case "create":
            {
                var dto = _json.Deserialize<CreateUserDto>(body);
                if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Request body is empty"); return; }
                var result = ServiceLocator.UserService.CreateUser(tenantId, requestingUserId, dto);
                ctx.Response.StatusCode = result.Success ? 201 : 400;
                WriteResult(ctx, result);
                break;
            }
            case "invite":
            {
                var dto = _json.Deserialize<InviteUserDto>(body);
                if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Request body is empty"); return; }
                var result = ServiceLocator.UserService.InviteUser(tenantId, requestingUserId, dto);
                ctx.Response.StatusCode = result.Success ? 201 : 400;
                WriteResult(ctx, result);
                break;
            }
            case "accept_invite":
            {
                var dto = _json.Deserialize<AcceptInviteDto>(body);
                if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Request body is empty"); return; }
                var result = ServiceLocator.UserService.AcceptInvitation(tenantId, dto);
                ctx.Response.StatusCode = result.Success ? 200 : 400;
                WriteResult(ctx, result);
                break;
            }
            case "change_password":
            {
                var dto = _json.Deserialize<ChangePasswordDto>(body);
                if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Request body is empty"); return; }
                dto.UserId = requestingUserId;    // always change own password via this action
                var result = ServiceLocator.UserService.ChangePassword(tenantId, dto);
                WriteResult(ctx, result);
                break;
            }
            case "reset_password":
            {
                var dto = _json.Deserialize<ResetPasswordDto>(body);
                if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Request body is empty"); return; }
                var result = ServiceLocator.UserService.ResetPassword(tenantId, dto);
                WriteResult(ctx, result);
                break;
            }
            case "unlock":
            {
                var d = _json.Deserialize<System.Collections.Generic.Dictionary<string, string>>(body);
                if (d == null || !d.ContainsKey("userId") || !Guid.TryParse(d["userId"], out var uid))
                { WriteError(ctx, 400, "ERR_BAD_REQUEST", "userId required"); return; }
                var result = ServiceLocator.UserService.UnlockUser(tenantId, uid);
                WriteResult(ctx, result);
                break;
            }
            default:
                WriteError(ctx, 400, "ERR_UNKNOWN_ACTION", "Unknown action");
                break;
        }
    }

    // ── PUT ──────────────────────────────────────────────────────────────

    private void HandlePut(HttpContext ctx, Guid tenantId, Guid requestingUserId, string action)
    {
        var body = ReadBody(ctx);
        var dto = _json.Deserialize<UpdateUserDto>(body);
        if (dto == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Request body is empty"); return; }
        var result = ServiceLocator.UserService.UpdateUser(tenantId, requestingUserId, dto);
        WriteResult(ctx, result);
    }

    // ── DELETE ───────────────────────────────────────────────────────────

    private void HandleDelete(HttpContext ctx, Guid tenantId, Guid requestingUserId, string action)
    {
        var idStr = ctx.Request.QueryString["id"];
        if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var userId))
        { WriteError(ctx, 400, "ERR_BAD_REQUEST", "id parameter is required"); return; }
        var result = ServiceLocator.UserService.DeleteUser(tenantId, requestingUserId, userId);
        WriteResult(ctx, result);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

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

    private static void RequireAdminGet(HttpContext ctx,
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
