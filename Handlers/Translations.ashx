<%@ WebHandler Language="C#" Class="TranslationsHandler" %>

using System;
using System.Web;
using System.Web.Script.Serialization;
using EidUbahle.CrossCutting;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Security;

/// <summary>
/// /Handlers/Translations.ashx – serves translation bundles to the client.
/// GET  ?lang=en         → returns bundle for language + direction
/// POST ?action=upsert   → saves a single translation
/// POST ?action=import   → bulk import JSON
/// POST ?action=export   → export JSON for a language
/// </summary>
public class TranslationsHandler : IHttpHandler
{
    private static readonly JavaScriptSerializer _json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
    public bool IsReusable => false;

    public void ProcessRequest(HttpContext ctx)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.Cache.SetCacheability(HttpCacheability.NoCache);

        var method = ctx.Request.HttpMethod.ToUpperInvariant();
        var action = ctx.Request.QueryString["action"]?.ToLowerInvariant();

        try
        {
            if (method == "GET")
            {
                ServeBundle(ctx);
                return;
            }

            // POST actions require auth
            var claims = ctx.Items["JwtClaims"] as System.Collections.Generic.Dictionary<string, string>;
            if (claims == null) { ctx.Response.StatusCode = 401; ctx.Response.Write("{\"success\":false}"); return; }

            switch (action)
            {
                case "upsert":      UpsertTranslation(ctx, claims); break;
                case "import":      ImportTranslations(ctx, claims); break;
                case "addlanguage": AddLanguage(ctx, claims); break;
                default: ctx.Response.StatusCode = 400; ctx.Response.Write("{\"success\":false,\"message\":\"Unknown action\"}"); break;
            }
        }
        catch (Exception ex)
        {
            ctx.Response.StatusCode = 500;
            ctx.Response.Write(_json.Serialize(new { success = false, message = ConfigHelper.IsProduction ? "Server error" : ex.Message }));
        }
    }

    private void ServeBundle(HttpContext ctx)
    {
        var lang = ctx.Request.QueryString["lang"] ?? "en";
        Guid? tenantId = null;
        var claims = ctx.Items["JwtClaims"] as System.Collections.Generic.Dictionary<string, string>;
        if (claims != null && Guid.TryParse(claims.GetValueOrDefault("tid"), out var tid))
            tenantId = tid;

        var svc  = ServiceLocator.TranslationService;
        var bundle = svc.GetBundleDto(lang, tenantId);

        // Add direction hint for RTL detection
        if (!bundle.Translations.ContainsKey("__dir"))
            bundle.Translations["__dir"] = bundle.Direction;

        ctx.Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(5));
        ctx.Response.Write(_json.Serialize(new ApiResponseDto<object>
        {
            Success = true,
            Data = new { translations = bundle.Translations, direction = bundle.Direction, languageCode = bundle.LanguageCode }
        }));
    }

    private void UpsertTranslation(HttpContext ctx, System.Collections.Generic.Dictionary<string, string> claims)
    {
        var body = ReadBody(ctx);
        var req  = _json.Deserialize<System.Collections.Generic.Dictionary<string, string>>(body);
        if (req == null) { ctx.Response.StatusCode = 400; ctx.Response.Write("{\"success\":false}"); return; }

        Guid? tenantId = null;
        if (Guid.TryParse(claims.GetValueOrDefault("tid"), out var tid)) tenantId = tid;

        var t = new EidUbahle.Domain.Entities.Translation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LanguageCode = req.GetValueOrDefault("languageCode") ?? "en",
            TranslationKey = req.GetValueOrDefault("key") ?? "",
            Text = req.GetValueOrDefault("text") ?? "",
            Module = req.GetValueOrDefault("module") ?? "General",
            IsCustom = tenantId.HasValue,
            UpdatedAt = DateTime.UtcNow
        };

        ServiceLocator.TranslationService.Upsert(t);
        ctx.Response.Write(_json.Serialize(new { success = true }));
    }

    private void ImportTranslations(HttpContext ctx, System.Collections.Generic.Dictionary<string, string> claims)
    {
        var body = ReadBody(ctx);
        var rows = _json.Deserialize<System.Collections.Generic.List<TranslationImportRowDto>>(body);
        if (rows == null) { ctx.Response.StatusCode = 400; ctx.Response.Write("{\"success\":false}"); return; }

        Guid? tenantId = null;
        if (Guid.TryParse(claims.GetValueOrDefault("tid"), out var tid)) tenantId = tid;

        var count = ServiceLocator.TranslationService.BulkImport(rows, tenantId);
        ctx.Response.Write(_json.Serialize(new { success = true, count }));
    }

    private void AddLanguage(HttpContext ctx, System.Collections.Generic.Dictionary<string, string> claims)
    {
        var body = ReadBody(ctx);
        var req  = _json.Deserialize<EidUbahle.Domain.Entities.Language>(body);
        if (req == null) { ctx.Response.StatusCode = 400; ctx.Response.Write("{\"success\":false}"); return; }

        ServiceLocator.TranslationService.AddLanguage(req);
        ctx.Response.Write(_json.Serialize(new { success = true }));
    }

    private static string ReadBody(HttpContext ctx)
    {
        using (var r = new System.IO.StreamReader(ctx.Request.InputStream))
            return r.ReadToEnd();
    }
}
