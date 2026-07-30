using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using EidUbahle.Domain.DTOs;
using EidUbahle.Domain.Entities;

namespace EidUbahle.Infrastructure.Security
{
    /// <summary>
    /// Generates and validates HS256 JWT tokens without external libraries
    /// to maintain pure WebForms compatibility.
    /// </summary>
    public static class JwtService
    {
        private static string SecretKey => ConfigHelper.JwtSecretKey;
        private static int AccessTokenMinutes => ConfigHelper.JwtAccessTokenMinutes;
        private static int RefreshTokenDays => ConfigHelper.JwtRefreshTokenDays;

        // ── Build Access Token ──────────────────────────────────────────
        public static string GenerateAccessToken(UserClaimsDto claims)
        {
            var header = Base64UrlEncode(
                Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));

            var payload = new
            {
                sub = claims.UserId.ToString(),
                tid = claims.TenantId.ToString(),
                usr = claims.Username,
                nam = claims.FullName,
                eml = claims.Email,
                lng = claims.LanguageCode,
                adm = claims.IsTenantAdmin,
                sad = claims.IsSuperAdmin,
                cid = claims.ActiveCompanyId?.ToString(),
                bid = claims.ActiveBranchId?.ToString(),
                prm = claims.Permissions,
                iat = ToUnixSeconds(DateTime.UtcNow),
                exp = ToUnixSeconds(DateTime.UtcNow.AddMinutes(AccessTokenMinutes)),
                iss = "eidubahle",
                aud = "eidubahle-client"
            };

            var payloadJson = SimpleJson(payload);
            var payloadEncoded = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
            var signingInput = $"{header}.{payloadEncoded}";
            var signature = Sign(signingInput);

            return $"{signingInput}.{signature}";
        }

        // ── Validate & Parse Token ──────────────────────────────────────
        public static TokenValidationResult ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return TokenValidationResult.Invalid("Token is empty");

            var parts = token.Split('.');
            if (parts.Length != 3)
                return TokenValidationResult.Invalid("Malformed token");

            var signingInput = $"{parts[0]}.{parts[1]}";
            var expectedSig = Sign(signingInput);
            if (!ConstantTimeEquals(expectedSig, parts[2]))
                return TokenValidationResult.Invalid("Invalid signature");

            try
            {
                var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
                var claims = ParseClaims(payloadJson);
                var exp = claims.ContainsKey("exp") ? long.Parse(claims["exp"]) : 0L;
                if (exp > 0 && ToUnixSeconds(DateTime.UtcNow) > exp)
                    return TokenValidationResult.Invalid("Token expired");

                return TokenValidationResult.Valid(claims);
            }
            catch (Exception ex)
            {
                return TokenValidationResult.Invalid($"Parse error: {ex.Message}");
            }
        }

        // ── Offline Validation (cached claims, no secret needed) ────────
        /// <summary>
        /// Validates the token structure and expiry from cached claims stored in IndexedDB.
        /// Full signature validation happens on the server; this is for offline UI gating.
        /// </summary>
        public static OfflineTokenInfo DecodeWithoutValidation(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            var parts = token.Split('.');
            if (parts.Length != 3) return null;
            try
            {
                var json = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
                var claims = ParseClaims(json);
                return new OfflineTokenInfo
                {
                    UserId = claims.GetValueOrDefault("sub"),
                    TenantId = claims.GetValueOrDefault("tid"),
                    Username = claims.GetValueOrDefault("usr"),
                    ExpiresAt = FromUnixSeconds(long.TryParse(claims.GetValueOrDefault("exp"), out var e) ? e : 0)
                };
            }
            catch { return null; }
        }

        // ── Refresh Token (opaque) ──────────────────────────────────────
        public static string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return Base64UrlEncode(bytes);
        }

        // ── Helpers ─────────────────────────────────────────────────────
        private static string Sign(string input)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Base64UrlEncode(hash);
            }
        }

        private static string Base64UrlEncode(byte[] data) =>
            Convert.ToBase64String(data)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

        private static byte[] Base64UrlDecode(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }

        private static long ToUnixSeconds(DateTime dt) =>
            (long)(dt.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

        private static DateTime FromUnixSeconds(long seconds) =>
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(seconds);

        private static bool ConstantTimeEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            int result = 0;
            for (int i = 0; i < a.Length; i++) result |= a[i] ^ b[i];
            return result == 0;
        }

        // Minimal JSON serializer to avoid external dependencies in App_Code
        private static string SimpleJson(object obj)
        {
            // Use System.Web.Script.Serialization available in WebForms
            var ser = new System.Web.Script.Serialization.JavaScriptSerializer();
            ser.MaxJsonLength = int.MaxValue;
            return ser.Serialize(obj);
        }

        private static Dictionary<string, string> ParseClaims(string json)
        {
            var ser = new System.Web.Script.Serialization.JavaScriptSerializer();
            var dict = ser.Deserialize<Dictionary<string, object>>(json);
            var result = new Dictionary<string, string>();
            foreach (var kv in dict)
                result[kv.Key] = kv.Value?.ToString();
            return result;
        }
    }

    public class TokenValidationResult
    {
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; }
        public Dictionary<string, string> Claims { get; private set; }

        public static TokenValidationResult Valid(Dictionary<string, string> claims) =>
            new TokenValidationResult { IsValid = true, Claims = claims };

        public static TokenValidationResult Invalid(string msg) =>
            new TokenValidationResult { IsValid = false, ErrorMessage = msg };
    }

    public class OfflineTokenInfo
    {
        public string UserId { get; set; }
        public string TenantId { get; set; }
        public string Username { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }
}
