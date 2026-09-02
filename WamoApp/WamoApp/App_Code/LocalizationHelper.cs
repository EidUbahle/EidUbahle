using System;
using System.Collections.Generic;
using System.Data;
using System.Web;

namespace WamoApp
{
    public static class LocalizationHelper
    {
        public static string GetCurrentLanguage()
        {
            var context = HttpContext.Current;
            if (context != null)
            {
                var q = context.Request.QueryString["lang"];
                if (!string.IsNullOrWhiteSpace(q)) { SetLanguageCookie(q); return q; }
                var c = context.Request.Cookies["WAMO_LANG"];
                if (c != null && !string.IsNullOrWhiteSpace(c.Value)) return c.Value;
            }
            return System.Configuration.ConfigurationManager.AppSettings["DefaultLanguage"] ?? "en";
        }

        public static void SetLanguageCookie(string code)
        {
            if (HttpContext.Current == null) return;
            HttpContext.Current.Response.Cookies.Set(new HttpCookie("WAMO_LANG", code) { HttpOnly = false, Secure = HttpContext.Current.Request.IsSecureConnection || SecurityHelper.IsHttpsRequired(), Expires = DateTime.UtcNow.AddYears(1) });
        }

        public static bool IsRightToLeft(string code) => string.Equals(code, "ar", StringComparison.OrdinalIgnoreCase);
        public static List<Dictionary<string, object>> GetLanguages() => DbHelper.ToDictionaryList(DbHelper.ExecuteDataTable("SELECT LanguageCode, Name, NativeName, IsDefault, IsRtl, IsActive FROM Languages WHERE IsActive = 1 ORDER BY SortOrder, Name", CommandType.Text));

        public static Dictionary<string, string> GetTranslations(string languageCode)
        {
            var table = DbHelper.ExecuteDataTable("SELECT TranslationKey, TranslationValue FROM Translations WHERE LanguageCode = @LanguageCode", CommandType.Text, new System.Data.SqlClient.SqlParameter("@LanguageCode", languageCode));
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in table.Rows) data[row["TranslationKey"].ToString()] = row["TranslationValue"].ToString();
            return data;
        }
    }
}
