using System;
using System.Collections.Generic;

namespace EidUbahle.Domain.DTOs
{
    // ─── Users ──────────────────────────────────────────────────────────

    public class UserListItemDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsActive { get; set; }
        public bool IsTenantAdmin { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> RoleNames { get; set; } = new List<string>();
        public List<string> BranchNames { get; set; } = new List<string>();
    }

    public class UserDetailDto : UserListItemDto
    {
        public Guid TenantId { get; set; }
        public string LanguageCode { get; set; }
        public string ThemeMode { get; set; }
        public string ActiveLayout { get; set; }
        public string AccentColor { get; set; }
        public bool IsSuperAdmin { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockedUntil { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<UserRoleAssignmentDto> Roles { get; set; } = new List<UserRoleAssignmentDto>();
        public List<UserBranchAssignmentDto> Branches { get; set; } = new List<UserBranchAssignmentDto>();
    }

    public class CreateUserDto
    {
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; }
        public bool IsTenantAdmin { get; set; }
        public string LanguageCode { get; set; } = "en";
        public List<Guid> RoleIds { get; set; } = new List<Guid>();
        public List<UserBranchAssignmentDto> Branches { get; set; } = new List<UserBranchAssignmentDto>();
    }

    public class UpdateUserDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsTenantAdmin { get; set; }
        public bool IsActive { get; set; }
        public string LanguageCode { get; set; }
        public string ThemeMode { get; set; }
        public string ActiveLayout { get; set; }
        public string AccentColor { get; set; }
        public List<Guid> RoleIds { get; set; } = new List<Guid>();
        public List<UserBranchAssignmentDto> Branches { get; set; } = new List<UserBranchAssignmentDto>();
    }

    public class ChangePasswordDto
    {
        public Guid UserId { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class ResetPasswordDto
    {
        public Guid UserId { get; set; }
        public string NewPassword { get; set; }
    }

    public class InviteUserDto
    {
        public string Email { get; set; }
        public string FullName { get; set; }
        public List<Guid> RoleIds { get; set; } = new List<Guid>();
        public List<UserBranchAssignmentDto> Branches { get; set; } = new List<UserBranchAssignmentDto>();
    }

    public class AcceptInviteDto
    {
        public string Token { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
    }

    public class UserRoleAssignmentDto
    {
        public Guid UserRoleId { get; set; }
        public Guid RoleId { get; set; }
        public string RoleName { get; set; }
        public Guid? CompanyId { get; set; }
        public string CompanyName { get; set; }
        public Guid? BranchId { get; set; }
        public string BranchName { get; set; }
    }

    public class UserBranchAssignmentDto
    {
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; }
        public Guid BranchId { get; set; }
        public string BranchName { get; set; }
        public string BranchCode { get; set; }
    }

    // ─── Roles & Permissions ────────────────────────────────────────────

    public class RoleListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RoleDetailDto : RoleListItemDto
    {
        public List<RolePermissionDto> Permissions { get; set; } = new List<RolePermissionDto>();
    }

    public class RolePermissionDto
    {
        public Guid PermissionId { get; set; }
        public string Module { get; set; }
        public string Feature { get; set; }
        public string Action { get; set; }
        public string PermissionKey { get; set; }
        public bool IsGranted { get; set; }
    }

    public class PermissionDto
    {
        public Guid Id { get; set; }
        public string Module { get; set; }
        public string Feature { get; set; }
        public string Action { get; set; }
        public string PermissionKey { get; set; }
    }

    public class PermissionMatrixDto
    {
        public List<PermissionGroupDto> Groups { get; set; } = new List<PermissionGroupDto>();
    }

    public class PermissionGroupDto
    {
        public string Module { get; set; }
        public List<PermissionFeatureDto> Features { get; set; } = new List<PermissionFeatureDto>();
    }

    public class PermissionFeatureDto
    {
        public string Feature { get; set; }
        public List<PermissionDto> Actions { get; set; } = new List<PermissionDto>();
    }

    public class CreateRoleDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Guid> PermissionIds { get; set; } = new List<Guid>();
    }

    public class UpdateRoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public List<Guid> PermissionIds { get; set; } = new List<Guid>();
    }

    // ─── Companies & Branches ───────────────────────────────────────────

    public class CompanyListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string LegalName { get; set; }
        public string RegistrationNumber { get; set; }
        public string TaxNumber { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string LogoUrl { get; set; }
        public string DefaultCurrencyCode { get; set; }
        public string AccountingBasis { get; set; }
        public bool IsActive { get; set; }
        public int BranchCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CompanyDetailDto : CompanyListItemDto
    {
        public Guid TenantId { get; set; }
        public string Address { get; set; }
        public string Website { get; set; }
        public string FiscalYearStart { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<BranchDto> Branches { get; set; } = new List<BranchDto>();
    }

    public class CreateCompanyDto
    {
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
        public string DefaultCurrencyCode { get; set; } = "USD";
        public string FiscalYearStart { get; set; } = "01-01";
        public string AccountingBasis { get; set; } = "Accrual";
    }

    public class UpdateCompanyDto : CreateCompanyDto
    {
        public Guid Id { get; set; }
        public bool IsActive { get; set; }
        public string LogoUrl { get; set; }
    }

    public class BranchDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public bool IsHeadOffice { get; set; }
        public bool IsActive { get; set; }
        public int UserCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateBranchDto
    {
        public Guid CompanyId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public bool IsHeadOffice { get; set; }
    }

    public class UpdateBranchDto : CreateBranchDto
    {
        public Guid Id { get; set; }
        public bool IsActive { get; set; }
    }

    // ─── Tenant Settings ────────────────────────────────────────────────

    public class TenantSettingsDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Subdomain { get; set; }
        public string LogoUrl { get; set; }
        public string AccentColor { get; set; }
        public string ThemeMode { get; set; }
        public string ActiveLayout { get; set; }
        public string DefaultLanguageCode { get; set; }
        public string DefaultCurrencyCode { get; set; }
        public string TimeZone { get; set; }
        public int MaxUsers { get; set; }
        public int MaxCompanies { get; set; }
        public bool IsActive { get; set; }
        public DateTime? TrialEndsAt { get; set; }
        public SubscriptionInfoDto Subscription { get; set; }
        public int CurrentUserCount { get; set; }
        public int CurrentCompanyCount { get; set; }
    }

    public class UpdateTenantSettingsDto
    {
        public string Name { get; set; }
        public string LogoUrl { get; set; }
        public string AccentColor { get; set; }
        public string ThemeMode { get; set; }
        public string ActiveLayout { get; set; }
        public string DefaultLanguageCode { get; set; }
        public string DefaultCurrencyCode { get; set; }
        public string TimeZone { get; set; }
    }

    public class SubscriptionInfoDto
    {
        public string PlanName { get; set; }
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxUsers { get; set; }
        public int MaxCompanies { get; set; }
        public int MaxBranches { get; set; }
        public long StorageLimitBytes { get; set; }
        public string EnabledModules { get; set; }
    }

    // ─── Onboarding ─────────────────────────────────────────────────────

    public class OnboardingStatusDto
    {
        public bool HasCompany { get; set; }
        public bool HasBranch { get; set; }
        public bool HasNonAdminUser { get; set; }
        public bool IsComplete { get; set; }
        public int CurrentStep { get; set; }    // 1=company, 2=branch, 3=invite, 4=done
    }

    public class OnboardingStep1Dto   // Company setup
    {
        public string CompanyName { get; set; }
        public string LegalName { get; set; }
        public string RegistrationNumber { get; set; }
        public string TaxNumber { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string DefaultCurrencyCode { get; set; } = "USD";
        public string FiscalYearStart { get; set; } = "01-01";
        public string AccountingBasis { get; set; } = "Accrual";
    }

    public class OnboardingStep2Dto   // First branch
    {
        public Guid CompanyId { get; set; }
        public string BranchName { get; set; }
        public string BranchCode { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
    }

    public class OnboardingStep3Dto   // Tenant branding
    {
        public string TenantName { get; set; }
        public string AccentColor { get; set; }
        public string ThemeMode { get; set; }
        public string ActiveLayout { get; set; }
        public string DefaultLanguageCode { get; set; }
        public string DefaultCurrencyCode { get; set; }
        public string TimeZone { get; set; }
    }

    // ─── Audit Log ──────────────────────────────────────────────────────

    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? UserId { get; set; }
        public string Username { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public Guid? EntityId { get; set; }
        public string OldValues { get; set; }
        public string NewValues { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ─── User Invitation ────────────────────────────────────────────────

    public class InvitationDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Status { get; set; }       // Pending | Accepted | Expired | Cancelled
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string InvitedByName { get; set; }
    }
}
