using System;
using System.Collections.Generic;

namespace EidUbahle.Domain.DTOs
{
    // ─── Auth ───────────────────────────────────────────────────

    public class LoginRequestDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string DeviceId { get; set; }
        public string DeviceInfo { get; set; }
        public bool RememberMe { get; set; }
        public string TotpCode { get; set; }           // 2FA
    }

    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime AccessTokenExpiry { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }
        public UserClaimsDto UserClaims { get; set; }
        public bool Require2FA { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
    }

    public class UserClaimsDto
    {
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
        public string LanguageCode { get; set; }
        public string ThemeMode { get; set; }
        public string ActiveLayout { get; set; }
        public string AccentColor { get; set; }
        public bool IsTenantAdmin { get; set; }
        public bool IsSuperAdmin { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
        public List<CompanyBranchDto> CompanyBranches { get; set; } = new List<CompanyBranchDto>();
        public Guid? ActiveCompanyId { get; set; }
        public Guid? ActiveBranchId { get; set; }
        public string ActiveCompanyName { get; set; }
        public string ActiveBranchName { get; set; }
        public string TenantSubdomain { get; set; }
        public string TenantLogoUrl { get; set; }
        public string TenantAccentColor { get; set; }
        public string TenantLayout { get; set; }
    }

    public class CompanyBranchDto
    {
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; }
        public Guid BranchId { get; set; }
        public string BranchName { get; set; }
        public string BranchCode { get; set; }
    }

    public class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; }
        public string DeviceId { get; set; }
    }

    // ─── Translations ────────────────────────────────────────────

    public class TranslationBundleDto
    {
        public string LanguageCode { get; set; }
        public string Direction { get; set; }
        public DateTime BundleTimestamp { get; set; }
        public Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();
    }

    public class TranslationImportRowDto
    {
        public string Key { get; set; }
        public string Module { get; set; }
        public Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();
    }

    // ─── API Responses ───────────────────────────────────────────

    public class ApiResponseDto<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; }
        public T Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public static ApiResponseDto<T> Ok(T data, string message = null) =>
            new ApiResponseDto<T> { Success = true, Data = data, Message = message };

        public static ApiResponseDto<T> Fail(string message, string code = null) =>
            new ApiResponseDto<T> { Success = false, Message = message, ErrorCode = code };
    }

    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }

    // ─── Sync ────────────────────────────────────────────────────

    public class SyncPushRequestDto
    {
        public string DeviceId { get; set; }
        public DateTime LastSyncAt { get; set; }
        public List<SyncRecordDto> Records { get; set; } = new List<SyncRecordDto>();
    }

    public class SyncRecordDto
    {
        public string EntityType { get; set; }
        public Guid EntityId { get; set; }
        public string Operation { get; set; }          // Create | Update | Delete
        public string PayloadJson { get; set; }
        public byte[] BaseVersion { get; set; }
        public DateTime ClientTimestamp { get; set; }
    }

    public class SyncPushResponseDto
    {
        public bool Success { get; set; }
        public int Accepted { get; set; }
        public int Rejected { get; set; }
        public List<SyncConflictDto> Conflicts { get; set; } = new List<SyncConflictDto>();
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class SyncPullResponseDto
    {
        public bool Success { get; set; }
        public DateTime ServerTimestamp { get; set; }
        public List<SyncRecordDto> Records { get; set; } = new List<SyncRecordDto>();
        public bool HasMore { get; set; }
        public string ContinuationToken { get; set; }
    }

    public class SyncConflictDto
    {
        public Guid ConflictId { get; set; }
        public string EntityType { get; set; }
        public Guid EntityId { get; set; }
        public string ClientJson { get; set; }
        public string ServerJson { get; set; }
        public string SuggestedResolution { get; set; }
    }
}
