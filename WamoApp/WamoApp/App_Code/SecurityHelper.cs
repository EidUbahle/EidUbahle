using System;
using System.Configuration;
using System.Globalization;
using System.Security.Cryptography;
using System.Web;

namespace WamoApp
{
    public static class SecurityHelper
    {
        public static void ApplySecurityHeaders(HttpResponse response)
        {
            if (response == null) return;
            response.Headers["X-Content-Type-Options"] = "nosniff";
            response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            response.Headers["Permissions-Policy"] = "geolocation=(self), microphone=(), camera=()";
            response.Headers["Content-Security-Policy"] = "default-src 'self' https: data: 'unsafe-inline' 'unsafe-eval'; frame-ancestors 'self'; object-src 'none'; base-uri 'self';";
            if (IsHttpsRequired() && HttpContext.Current != null && HttpContext.Current.Request.IsSecureConnection)
                response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        public static bool IsHttpsRequired() => string.Equals(ConfigurationManager.AppSettings["RequireHttps"], "true", StringComparison.OrdinalIgnoreCase);

        public static string GetOrCreateCsrfToken()
        {
            var context = HttpContext.Current;
            if (context == null) return string.Empty;
            var cookie = context.Request.Cookies["WAMO_CSRF"];
            if (cookie != null && !string.IsNullOrWhiteSpace(cookie.Value)) return cookie.Value;
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            var token = Convert.ToBase64String(bytes);
            context.Response.Cookies.Set(new HttpCookie("WAMO_CSRF", token) { HttpOnly = false, Secure = context.Request.IsSecureConnection || IsHttpsRequired(), SameSite = SameSiteMode.Lax, Path = "/" });
            return token;
        }

        public static void ValidateCsrfOrThrow()
        {
            var context = HttpContext.Current;
            if (context == null) throw new InvalidOperationException("Invalid request context.");
            var header = context.Request.Headers[ConfigurationManager.AppSettings["CsrfHeaderName"] ?? "X-CSRF-Token"];
            var cookie = context.Request.Cookies["WAMO_CSRF"] != null ? context.Request.Cookies["WAMO_CSRF"].Value : string.Empty;
            if (string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(cookie) || !FixedTimeEquals(header, cookie)) throw new UnauthorizedAccessException("CSRF validation failed.");
        }

        public static bool FixedTimeEquals(string left, string right)
        {
            var a = System.Text.Encoding.UTF8.GetBytes(left ?? string.Empty);
            var b = System.Text.Encoding.UTF8.GetBytes(right ?? string.Empty);
            if (a.Length != b.Length) return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }

        public static string HtmlEncode(string value) => HttpUtility.HtmlEncode(value ?? string.Empty);
        public static string GetIpAddress() => HttpContext.Current == null ? string.Empty : (HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] ?? HttpContext.Current.Request.UserHostAddress ?? string.Empty).Split(',')[0].Trim();
        public static string GetUserAgent() => HttpContext.Current == null ? string.Empty : (HttpContext.Current.Request.UserAgent ?? string.Empty);

        public static void EnsureRequestCulture()
        {
            var cultureCode = LocalizationHelper.GetCurrentLanguage();
            var culture = new CultureInfo(cultureCode == "ar" ? "ar-SA" : cultureCode);
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
        }
    }
}
