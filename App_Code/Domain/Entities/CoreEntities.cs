using System;
using System.Collections.Generic;

namespace EidUbahle.Domain.Entities
{
    // ─────────────────────────────────────────────────────────────
    //  SaaS / Multi-Tenancy Entities
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Top-level SaaS tenant.  Every piece of data is scoped to a Tenant.
    /// </summary>
    public class Tenant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Subdomain { get; set; }           // e.g. acme.eidubahle.com
        public string LogoUrl { get; set; }
        public string AccentColor { get; set; } = "#2563EB";
        public string ThemeMode { get; set; } = "light"; // light | dark | auto
        public string ActiveLayout { get; set; } = "classic"; // classic | topnav | compact
        public Guid SubscriptionPlanId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime TrialEndsAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public string DefaultLanguageCode { get; set; } = "en";
        public string DefaultCurrencyCode { get; set; } = "USD";
        public string TimeZone { get; set; } = "UTC";
        public int MaxUsers { get; set; } = 5;
        public int MaxCompanies { get; set; } = 1;
        public long StorageLimitBytes { get; set; } = 1073741824; // 1 GB
    }

    /// <summary>
    /// A legal entity / company within a tenant.
    /// </summary>
    public class Company
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string Name { get; set; }
        public string LegalName { get; set; }
        public string RegistrationNumber { get; set; }
        public string TaxNumber { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string LogoUrl { get; set; }
        public string DefaultCurrencyCode { get; set; } = "USD";
        public string FiscalYearStart { get; set; } = "01-01"; // MM-dd
        public string AccountingBasis { get; set; } = "Accrual"; // Accrual | Cash
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
    }

    /// <summary>
    /// A branch / location within a company.
    /// </summary>
    public class Branch
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid CompanyId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }              // used in document numbering
        public string Address { get; set; }
        public string Phone { get; set; }
        public bool IsHeadOffice { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  Users, Roles & Permissions
    // ─────────────────────────────────────────────────────────────

    public class AppUser
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; }
        public string Phone { get; set; }
        public string LanguageCode { get; set; } = "en";
        public string ThemeMode { get; set; } = "auto";    // auto | light | dark
        public string ActiveLayout { get; set; }           // overrides tenant layout
        public string AccentColor { get; set; }            // overrides tenant accent
        public bool IsTenantAdmin { get; set; } = false;
        public bool IsSuperAdmin { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public bool TwoFactorEnabled { get; set; } = false;
        public string TwoFactorSecret { get; set; }
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockedUntil { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LastSyncAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public byte[] Version { get; set; }
    }

    public class Role
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsSystem { get; set; } = false;    // built-in roles cannot be deleted
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
    }

    public class UserRole
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid? CompanyId { get; set; }           // null = all companies
        public Guid? BranchId { get; set; }            // null = all branches
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }

    public class Permission
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Module { get; set; }             // Accounting, Inventory, Sales …
        public string Feature { get; set; }            // Invoice, Payment …
        public string Action { get; set; }             // View, Create, Edit, Delete, Approve, Export
        public string PermissionKey { get; set; }      // accounting.invoice.create
    }

    public class RolePermission
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RoleId { get; set; }
        public Guid PermissionId { get; set; }
        public bool IsGranted { get; set; } = true;
    }

    public class UserCompanyBranch
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid CompanyId { get; set; }
        public Guid BranchId { get; set; }
    }

    // ─────────────────────────────────────────────────────────────
    //  Localization / Multi-Language
    // ─────────────────────────────────────────────────────────────

    public class Language
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Code { get; set; }               // en, so, ar, fr …
        public string Name { get; set; }               // English, Somali …
        public string NativeName { get; set; }         // English, Soomaali, العربية …
        public string Direction { get; set; } = "ltr"; // ltr | rtl
        public string FlagIcon { get; set; }           // emoji or icon class
        public bool IsDefault { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 100;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Translation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? TenantId { get; set; }            // null = system-wide
        public string LanguageCode { get; set; }
        public string TranslationKey { get; set; }
        public string Text { get; set; }
        public string Module { get; set; }             // grouping
        public bool IsCustom { get; set; } = false;   // tenant override of system translation
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public byte[] Version { get; set; }
    }

    // ─────────────────────────────────────────────────────────────
    //  Auth / Sessions
    // ─────────────────────────────────────────────────────────────

    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Token { get; set; }
        public string DeviceId { get; set; }
        public string DeviceInfo { get; set; }
        public string IpAddress { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class LoginHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public bool Success { get; set; }
        public string FailureReason { get; set; }
        public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    }

    // ─────────────────────────────────────────────────────────────
    //  SaaS Subscription
    // ─────────────────────────────────────────────────────────────

    public class SubscriptionPlan
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }               // Trial, Monthly, Annual, Enterprise
        public string Description { get; set; }
        public decimal PriceMonthly { get; set; }
        public decimal PriceAnnual { get; set; }
        public int MaxUsers { get; set; }
        public int MaxCompanies { get; set; }
        public int MaxBranches { get; set; }
        public long StorageLimitBytes { get; set; }
        public int TrialDays { get; set; }
        public string EnabledModules { get; set; }     // JSON array: ["Accounting","Inventory",…]
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class TenantSubscription
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid PlanId { get; set; }
        public string Status { get; set; }             // Active, Expired, Cancelled, Trial
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string PaymentProvider { get; set; }    // Stripe | PayPal
        public string ExternalSubscriptionId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // ─────────────────────────────────────────────────────────────
    //  Feature Flags (per tenant)
    // ─────────────────────────────────────────────────────────────

    public class FeatureFlag
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? TenantId { get; set; }            // null = global default
        public string FeatureKey { get; set; }         // module.feature e.g. payroll.enabled
        public bool IsEnabled { get; set; } = false;
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // ─────────────────────────────────────────────────────────────
    //  Sync / Offline
    // ─────────────────────────────────────────────────────────────

    public class SyncLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public string DeviceId { get; set; }
        public string SyncType { get; set; }           // Push | Pull | Full
        public string Status { get; set; }             // Success | Failed | Conflict | Partial
        public int RecordsPushed { get; set; }
        public int RecordsPulled { get; set; }
        public int ConflictsDetected { get; set; }
        public int ConflictsResolved { get; set; }
        public string ErrorDetails { get; set; }
        public long DurationMs { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }

    public class SyncConflict
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string EntityType { get; set; }         // Invoice, Journal …
        public Guid EntityId { get; set; }
        public string ClientJson { get; set; }
        public string ServerJson { get; set; }
        public string Resolution { get; set; }         // Pending | ServerWins | ClientWins | Manual
        public Guid? ResolvedBy { get; set; }
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public byte[] ClientVersion { get; set; }
        public byte[] ServerVersion { get; set; }
    }
}
