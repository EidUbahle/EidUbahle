using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using EidUbahle.Domain.Entities;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Security;
using EidUbahle.Infrastructure.Caching;

namespace EidUbahle.Infrastructure.Localization
{
    /// <summary>
    /// Server-side translation engine.
    /// Translations are loaded once per tenant/language and cached in memory.
    /// Zero hardcoded UI strings – all keys map to the Translations table.
    /// </summary>
    public class TranslationService
    {
        private readonly string _connectionString;
        private readonly IAppCache _cache;

        public TranslationService(string connectionString, IAppCache cache)
        {
            _connectionString = connectionString;
            _cache = cache;
        }

        // ── Get single translation ───────────────────────────────────────
        public string Get(string key, string languageCode, Guid? tenantId = null, params object[] args)
        {
            var bundle = GetBundle(languageCode, tenantId);
            string text = bundle.ContainsKey(key) ? bundle[key] : key; // fallback to key
            if (args != null && args.Length > 0)
            {
                try { text = string.Format(text, args); }
                catch { /* ignore format errors */ }
            }
            return text;
        }

        // ── Get full bundle (cached) ─────────────────────────────────────
        public Dictionary<string, string> GetBundle(string languageCode, Guid? tenantId = null)
        {
            string cacheKey = $"translations:{languageCode}:{tenantId?.ToString() ?? "system"}";
            return _cache.GetOrAdd(cacheKey, () => LoadFromDb(languageCode, tenantId), TimeSpan.FromMinutes(30));
        }

        // ── Get bundle as DTO (for client download) ──────────────────────
        public TranslationBundleDto GetBundleDto(string languageCode, Guid? tenantId = null)
        {
            var lang = GetLanguage(languageCode);
            return new TranslationBundleDto
            {
                LanguageCode = languageCode,
                Direction = lang?.Direction ?? "ltr",
                BundleTimestamp = DateTime.UtcNow,
                Translations = GetBundle(languageCode, tenantId)
            };
        }

        // ── Get all active languages ─────────────────────────────────────
        public List<Language> GetAllLanguages()
        {
            string cacheKey = "languages:all";
            return _cache.GetOrAdd(cacheKey, () => LoadLanguagesFromDb(), TimeSpan.FromMinutes(60));
        }

        public Language GetLanguage(string code)
        {
            return GetAllLanguages().FirstOrDefault(l => l.Code == code);
        }

        // ── Upsert translation ───────────────────────────────────────────
        public void Upsert(Translation t)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("sp_Translation_Upsert", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Id", t.Id);
                    cmd.Parameters.AddWithValue("@TenantId", (object)t.TenantId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LanguageCode", t.LanguageCode);
                    cmd.Parameters.AddWithValue("@TranslationKey", t.TranslationKey);
                    cmd.Parameters.AddWithValue("@Text", t.Text ?? "");
                    cmd.Parameters.AddWithValue("@Module", t.Module ?? "General");
                    cmd.Parameters.AddWithValue("@IsCustom", t.IsCustom);
                    cmd.ExecuteNonQuery();
                }
            }
            // Invalidate cache
            _cache.Remove($"translations:{t.LanguageCode}:{t.TenantId?.ToString() ?? "system"}");
        }

        // ── Delete translation ────────────────────────────────────────────
        public void Delete(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("DELETE FROM sys_Translations WHERE Id = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ── Add language ──────────────────────────────────────────────────
        public void AddLanguage(Language lang)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("sp_Language_Upsert", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Id", lang.Id);
                    cmd.Parameters.AddWithValue("@Code", lang.Code);
                    cmd.Parameters.AddWithValue("@Name", lang.Name);
                    cmd.Parameters.AddWithValue("@NativeName", lang.NativeName ?? lang.Name);
                    cmd.Parameters.AddWithValue("@Direction", lang.Direction ?? "ltr");
                    cmd.Parameters.AddWithValue("@FlagIcon", lang.FlagIcon ?? "");
                    cmd.Parameters.AddWithValue("@IsDefault", lang.IsDefault);
                    cmd.Parameters.AddWithValue("@IsActive", lang.IsActive);
                    cmd.Parameters.AddWithValue("@SortOrder", lang.SortOrder);
                    cmd.ExecuteNonQuery();
                }
            }
            _cache.Remove("languages:all");
        }

        // ── Bulk import translations ──────────────────────────────────────
        public int BulkImport(List<TranslationImportRowDto> rows, Guid? tenantId)
        {
            int count = 0;
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                foreach (var row in rows)
                {
                    foreach (var kv in row.Translations)
                    {
                        var t = new Translation
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            LanguageCode = kv.Key,
                            TranslationKey = row.Key,
                            Text = kv.Value,
                            Module = row.Module ?? "General",
                            IsCustom = tenantId.HasValue,
                            UpdatedAt = DateTime.UtcNow
                        };
                        using (var cmd = new SqlCommand("sp_Translation_Upsert", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Id", t.Id);
                            cmd.Parameters.AddWithValue("@TenantId", (object)t.TenantId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@LanguageCode", t.LanguageCode);
                            cmd.Parameters.AddWithValue("@TranslationKey", t.TranslationKey);
                            cmd.Parameters.AddWithValue("@Text", t.Text ?? "");
                            cmd.Parameters.AddWithValue("@Module", t.Module);
                            cmd.Parameters.AddWithValue("@IsCustom", t.IsCustom);
                            cmd.ExecuteNonQuery();
                        }
                        count++;
                    }
                }
            }
            // Clear all translation caches
            foreach (var lang in GetAllLanguages())
                _cache.Remove($"translations:{lang.Code}:{tenantId?.ToString() ?? "system"}");
            return count;
        }

        // ── Export as JSON ────────────────────────────────────────────────
        public string ExportJson(string languageCode, Guid? tenantId)
        {
            var bundle = GetBundle(languageCode, tenantId);
            var ser = new System.Web.Script.Serialization.JavaScriptSerializer();
            ser.MaxJsonLength = int.MaxValue;
            return ser.Serialize(bundle);
        }

        // ── Private: Load from DB ─────────────────────────────────────────
        private Dictionary<string, string> LoadFromDb(string languageCode, Guid? tenantId)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                const string sql = @"
                    SELECT TranslationKey, [Text]
                    FROM   sys_Translations
                    WHERE  LanguageCode = @Lang
                      AND  IsDeleted = 0
                      AND  (TenantId IS NULL OR TenantId = @TenantId)
                    ORDER BY IsCustom ASC  -- system first, tenant overrides last";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Lang", languageCode);
                    cmd.Parameters.AddWithValue("@TenantId", (object)tenantId ?? DBNull.Value);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            result[reader.GetString(0)] = reader.GetString(1);
                    }
                }
            }
            return result;
        }

        private List<Language> LoadLanguagesFromDb()
        {
            var list = new List<Language>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                const string sql = "SELECT Id,Code,Name,NativeName,Direction,FlagIcon,IsDefault,IsActive,SortOrder FROM sys_Languages WHERE IsActive=1 ORDER BY SortOrder,Name";
                using (var cmd = new SqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Language
                        {
                            Id = reader.GetGuid(0),
                            Code = reader.GetString(1),
                            Name = reader.GetString(2),
                            NativeName = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Direction = reader.IsDBNull(4) ? "ltr" : reader.GetString(4),
                            FlagIcon = reader.IsDBNull(5) ? null : reader.GetString(5),
                            IsDefault = reader.GetBoolean(6),
                            IsActive = reader.GetBoolean(7),
                            SortOrder = reader.GetInt32(8)
                        });
                    }
                }
            }
            return list;
        }
    }
}
