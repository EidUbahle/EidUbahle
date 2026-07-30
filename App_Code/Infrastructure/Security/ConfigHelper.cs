using System;
using System.Web;
using System.Configuration;

namespace EidUbahle.Infrastructure.Security
{
    /// <summary>
    /// Centralised configuration helper. All app settings come through here,
    /// making it easy to swap to Azure Key Vault or environment variables later.
    /// </summary>
    public static class ConfigHelper
    {
        public static string JwtSecretKey =>
            Get("Jwt:SecretKey") ?? throw new InvalidOperationException("Jwt:SecretKey is not configured");

        public static int JwtAccessTokenMinutes =>
            int.TryParse(Get("Jwt:AccessTokenMinutes"), out var v) ? v : 15;

        public static int JwtRefreshTokenDays =>
            int.TryParse(Get("Jwt:RefreshTokenDays"), out var v) ? v : 30;

        public static string DbConnectionString =>
            ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString
            ?? throw new InvalidOperationException("DefaultConnection not configured");

        public static string ApplicationName =>
            Get("App:Name") ?? "EidUbahle ERP";

        public static string ApplicationVersion =>
            Get("App:Version") ?? "1.0.0";

        public static string DefaultLanguage =>
            Get("App:DefaultLanguage") ?? "en";

        public static bool IsProduction =>
            string.Equals(Get("App:Environment"), "Production", StringComparison.OrdinalIgnoreCase);

        public static int MaxLoginAttempts =>
            int.TryParse(Get("Security:MaxLoginAttempts"), out var v) ? v : 5;

        public static int LockoutMinutes =>
            int.TryParse(Get("Security:LockoutMinutes"), out var v) ? v : 30;

        public static string StorageProvider =>
            Get("Storage:Provider") ?? "Local";

        public static string AzureBlobConnectionString =>
            Get("Storage:AzureBlobConnectionString");

        public static string LocalStoragePath =>
            Get("Storage:LocalPath") ?? HttpContext.Current?.Server.MapPath("~/App_Data/Uploads") ?? "App_Data/Uploads";

        private static string Get(string key) =>
            ConfigurationManager.AppSettings[key];
    }
}
