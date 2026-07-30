<%@ WebHandler Language="C#" Class="AuthHandler" %>

using System;
using System.IO;
using System.Web;
using System.Web.Script.Serialization;
using EidUbahle.CrossCutting;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Security;

/// <summary>
/// /Handlers/Auth.ashx – handles login, token refresh, and logout via AJAX.
/// No ScriptManager, no UpdatePanel – pure XHR/JSON.
/// </summary>
public class AuthHandler : IHttpHandler
{
    private static readonly JavaScriptSerializer _json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

    public bool IsReusable => false;

    public void ProcessRequest(HttpContext context)
    {
        context.Response.ContentType = "application/json";
        context.Response.Cache.SetNoStore();

        // CSRF / XHR guard
        if (!IsXhrRequest(context.Request))
        {
            WriteError(context, 400, "ERR_INVALID_REQUEST", "Invalid request");
            return;
        }

        var action = context.Request.QueryString["action"]?.ToLowerInvariant();
        try
        {
            switch (action)
            {
                case "login":   HandleLogin(context); break;
                case "refresh": HandleRefresh(context); break;
                case "logout":  HandleLogout(context); break;
                default: WriteError(context, 400, "ERR_UNKNOWN_ACTION", "Unknown action"); break;
            }
        }
        catch (Exception ex)
        {
            WriteError(context, 500, "ERR_SERVER", ConfigHelper.IsProduction ? "Internal server error" : ex.Message);
        }
    }

    private void HandleLogin(HttpContext ctx)
    {
        var body = ReadBody(ctx);
        LoginRequestDto req;
        try { req = _json.Deserialize<LoginRequestDto>(body); }
        catch { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Invalid request body"); return; }

        if (req == null) { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Request body is empty"); return; }

        var result = ServiceLocator.AuthService.Login(req, ctx.Request.UserHostAddress, ctx.Request.UserAgent);

        if (result.Success)
        {
            SetTokenCookie(ctx, "eid_access", result.AccessToken, result.AccessTokenExpiry);
            if (req.RememberMe)
                SetTokenCookie(ctx, "eid_refresh", result.RefreshToken, result.RefreshTokenExpiry);
        }

        ctx.Response.StatusCode = result.Success ? 200 : 401;
        ctx.Response.Write(_json.Serialize(result));
    }

    private void HandleRefresh(HttpContext ctx)
    {
        var body = ReadBody(ctx);
        RefreshTokenRequestDto req;
        try { req = _json.Deserialize<RefreshTokenRequestDto>(body); }
        catch { WriteError(ctx, 400, "ERR_BAD_REQUEST", "Invalid request body"); return; }

        if (string.IsNullOrEmpty(req?.RefreshToken))
        {
            var cookie = ctx.Request.Cookies["eid_refresh"];
            if (cookie != null) req = new RefreshTokenRequestDto { RefreshToken = cookie.Value };
        }

        if (req == null || string.IsNullOrEmpty(req.RefreshToken))
        { WriteError(ctx, 401, "ERR_NO_REFRESH_TOKEN", "Refresh token required"); return; }

        var result = ServiceLocator.AuthService.RefreshToken(req, ctx.Request.UserHostAddress);
        if (result.Success) SetTokenCookie(ctx, "eid_access", result.AccessToken, result.AccessTokenExpiry);
        ctx.Response.StatusCode = result.Success ? 200 : 401;
        ctx.Response.Write(_json.Serialize(result));
    }

    private void HandleLogout(HttpContext ctx)
    {
        var body = ReadBody(ctx);
        string refreshToken = null;
        try
        {
            var dict = _json.Deserialize<System.Collections.Generic.Dictionary<string, string>>(body);
            dict?.TryGetValue("refreshToken", out refreshToken);
        }
        catch { }

        if (string.IsNullOrEmpty(refreshToken))
        {
            var cookie = ctx.Request.Cookies["eid_refresh"];
            if (cookie != null) refreshToken = cookie.Value;
        }

        ServiceLocator.AuthService.Logout(refreshToken);
        ClearCookie(ctx, "eid_access");
        ClearCookie(ctx, "eid_refresh");
        ctx.Response.Write(_json.Serialize(new { success = true }));
    }

    private static string ReadBody(HttpContext ctx)
    {
        using (var reader = new System.IO.StreamReader(ctx.Request.InputStream))
            return reader.ReadToEnd();
    }

    private static void WriteError(HttpContext ctx, int status, string code, string msg)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.Write(_json.Serialize(new { success = false, errorCode = code, message = msg }));
    }

    private static bool IsXhrRequest(HttpRequest req) =>
        string.Equals(req.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
        || req.ContentType?.Contains("application/json") == true;

    private static void SetTokenCookie(HttpContext ctx, string name, string value, DateTime expires)
    {
        var cookie = new HttpCookie(name, value)
        {
            HttpOnly = true,
            Secure = ctx.Request.IsSecureConnection,
            SameSite = SameSiteMode.Strict,
            Expires = expires,
            Path = "/"
        };
        ctx.Response.Cookies.Add(cookie);
    }

    private static void ClearCookie(HttpContext ctx, string name)
    {
        var cookie = new HttpCookie(name, "")
        {
            HttpOnly = true,
            Secure = ctx.Request.IsSecureConnection,
            Expires = DateTime.UtcNow.AddDays(-1),
            Path = "/"
        };
        ctx.Response.Cookies.Add(cookie);
    }
}
