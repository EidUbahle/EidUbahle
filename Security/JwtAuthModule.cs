using System;
using System.Collections.Generic;
using System.Web;
using EidUbahle.Infrastructure.Security;

namespace EidUbahle.Security
{
    /// <summary>
    /// HTTP Module that validates JWT tokens on every request.
    /// Public paths (login, static files, AJAX auth endpoints) are whitelisted.
    /// Sets HttpContext.Current.Items["CurrentUser"] for downstream consumption.
    /// </summary>
    public class JwtAuthModule : IHttpModule
    {
        private static readonly HashSet<string> PublicPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/",
            "/default.aspx",
            "/pages/login.aspx",
            "/pages/error.aspx",
            "/handlers/auth.ashx",
            "/sw.js",
            "/manifest.webmanifest",
        };

        // Paths that require JWT but are not admin-only (handled by page code-behind)
        private static readonly HashSet<string> AuthenticatedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/pages/onboarding.aspx",
            "/admin/users.aspx",
            "/admin/roles.aspx",
            "/admin/companies.aspx",
            "/admin/tenantsettings.aspx",
            "/admin/translations.aspx",
        };

        private static readonly string[] PublicPrefixes = new[]
        {
            "/scripts/",
            "/styles/",
            "/images/",
            "/fonts/",
            "/offline/",
        };

        public void Init(HttpApplication context)
        {
            context.AuthenticateRequest += OnAuthenticateRequest;
            context.PostAuthenticateRequest += OnPostAuthenticateRequest;
        }

        private void OnAuthenticateRequest(object sender, EventArgs e)
        {
            var app = (HttpApplication)sender;
            var context = app.Context;
            var path = context.Request.AppRelativeCurrentExecutionFilePath
                              .TrimStart('~').ToLowerInvariant();

            // Allow public paths
            if (IsPublicPath(path)) return;

            var token = ExtractToken(context.Request);
            if (string.IsNullOrEmpty(token))
            {
                RedirectToLogin(context);
                return;
            }

            var result = JwtService.ValidateToken(token);
            if (!result.IsValid)
            {
                // Try to refresh via cookie (handled client-side; reject here)
                RedirectToLogin(context);
                return;
            }

            context.Items["JwtClaims"] = result.Claims;
        }

        private void OnPostAuthenticateRequest(object sender, EventArgs e)
        {
            // Additional per-request setup can go here
        }

        private static string ExtractToken(HttpRequest request)
        {
            // 1) Authorization: ******
            var auth = request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return auth.Substring(7).Trim();

            // 2) X-Auth-Token header (for AJAX calls)
            var header = request.Headers["X-Auth-Token"];
            if (!string.IsNullOrEmpty(header)) return header.Trim();

            // 3) Cookie (for page navigation fallback)
            var cookie = request.Cookies["eid_access"];
            if (cookie != null && !string.IsNullOrEmpty(cookie.Value)) return cookie.Value;

            return null;
        }

        private static bool IsPublicPath(string path)
        {
            if (PublicPaths.Contains(path)) return true;
            foreach (var prefix in PublicPrefixes)
                if (path.StartsWith(prefix)) return true;

            // Static file extensions
            var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".css" || ext == ".js" || ext == ".png" || ext == ".jpg" ||
                ext == ".ico" || ext == ".svg" || ext == ".woff" || ext == ".woff2" ||
                ext == ".ttf" || ext == ".eot" || ext == ".webmanifest") return true;

            return false;
        }

        private static void RedirectToLogin(HttpContext context)
        {
            // AJAX requests: return 401 JSON
            if (IsAjaxRequest(context.Request))
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                context.Response.Write("{\"success\":false,\"errorCode\":\"ERR_UNAUTHORIZED\",\"message\":\"Authentication required\"}");
                context.Response.End();
            }
            else
            {
                // Page requests: redirect to login
                var returnUrl = HttpUtility.UrlEncode(context.Request.RawUrl);
                context.Response.Redirect($"~/Pages/Login.aspx?returnUrl={returnUrl}", false);
                context.ApplicationInstance.CompleteRequest();
            }
        }

        private static bool IsAjaxRequest(HttpRequest request) =>
            string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
            || request.ContentType?.Contains("application/json") == true
            || request.Path.EndsWith(".ashx", StringComparison.OrdinalIgnoreCase);

        public void Dispose() { }
    }
}
