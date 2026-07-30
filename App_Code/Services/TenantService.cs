using System;
using System.Collections.Generic;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Caching;
using EidUbahle.Repositories;

namespace EidUbahle.Services
{
    /// <summary>
    /// Business logic for tenant settings, company and branch management,
    /// and the onboarding wizard workflow.
    /// </summary>
    public class TenantService
    {
        private readonly TenantRepository _repo;
        private readonly IAppCache _cache;

        public TenantService(string connectionString, IAppCache cache)
        {
            _repo = new TenantRepository(connectionString);
            _cache = cache;
        }

        // ── Tenant Settings ───────────────────────────────────────────────

        public ApiResponseDto<TenantSettingsDto> GetSettings(Guid tenantId)
        {
            var cacheKey = $"tenant_settings:{tenantId}";
            var data = _cache.GetOrAdd(cacheKey, () => _repo.GetSettings(tenantId), TimeSpan.FromMinutes(15));
            if (data == null)
                return ApiResponseDto<TenantSettingsDto>.Fail("Tenant not found", "ERR_NOT_FOUND");
            return ApiResponseDto<TenantSettingsDto>.Ok(data);
        }

        public ApiResponseDto<bool> UpdateSettings(Guid tenantId, UpdateTenantSettingsDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponseDto<bool>.Fail("Tenant name is required", "ERR_VALIDATION");

            _repo.UpdateSettings(tenantId, dto);
            _cache.Remove($"tenant_settings:{tenantId}");
            return ApiResponseDto<bool>.Ok(true, "Settings updated successfully");
        }

        // ── Onboarding ────────────────────────────────────────────────────

        public ApiResponseDto<OnboardingStatusDto> GetOnboardingStatus(Guid tenantId)
        {
            return ApiResponseDto<OnboardingStatusDto>.Ok(_repo.GetOnboardingStatus(tenantId));
        }

        public ApiResponseDto<Guid> OnboardingStep1(Guid tenantId, OnboardingStep1Dto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CompanyName))
                return ApiResponseDto<Guid>.Fail("Company name is required", "ERR_VALIDATION");

            var create = new CreateCompanyDto
            {
                Name = dto.CompanyName,
                LegalName = dto.LegalName,
                RegistrationNumber = dto.RegistrationNumber,
                TaxNumber = dto.TaxNumber,
                Country = dto.Country,
                City = dto.City,
                Address = dto.Address,
                Phone = dto.Phone,
                Email = dto.Email,
                DefaultCurrencyCode = dto.DefaultCurrencyCode ?? "USD",
                FiscalYearStart = dto.FiscalYearStart ?? "01-01",
                AccountingBasis = dto.AccountingBasis ?? "Accrual"
            };
            var id = _repo.CreateCompany(tenantId, create);
            return ApiResponseDto<Guid>.Ok(id, "Company created");
        }

        public ApiResponseDto<Guid> OnboardingStep2(Guid tenantId, OnboardingStep2Dto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.BranchName))
                return ApiResponseDto<Guid>.Fail("Branch name is required", "ERR_VALIDATION");
            if (dto.CompanyId == Guid.Empty)
                return ApiResponseDto<Guid>.Fail("Company is required", "ERR_VALIDATION");

            var create = new CreateBranchDto
            {
                CompanyId = dto.CompanyId,
                Name = dto.BranchName,
                Code = dto.BranchCode,
                Address = dto.Address,
                Phone = dto.Phone,
                IsHeadOffice = true
            };
            var id = _repo.CreateBranch(tenantId, create);
            return ApiResponseDto<Guid>.Ok(id, "Head office branch created");
        }

        public ApiResponseDto<bool> OnboardingStep3(Guid tenantId, OnboardingStep3Dto dto)
        {
            var update = new UpdateTenantSettingsDto
            {
                Name = dto.TenantName,
                AccentColor = dto.AccentColor,
                ThemeMode = dto.ThemeMode,
                ActiveLayout = dto.ActiveLayout,
                DefaultLanguageCode = dto.DefaultLanguageCode,
                DefaultCurrencyCode = dto.DefaultCurrencyCode,
                TimeZone = dto.TimeZone
            };
            _repo.UpdateSettings(tenantId, update);
            _cache.Remove($"tenant_settings:{tenantId}");
            return ApiResponseDto<bool>.Ok(true, "Branding configured");
        }

        // ── Companies ─────────────────────────────────────────────────────

        public ApiResponseDto<List<CompanyListItemDto>> GetCompanies(Guid tenantId, bool includeInactive = false)
        {
            return ApiResponseDto<List<CompanyListItemDto>>.Ok(_repo.GetCompanies(tenantId, includeInactive));
        }

        public ApiResponseDto<CompanyDetailDto> GetCompanyById(Guid tenantId, Guid companyId)
        {
            var co = _repo.GetCompanyById(tenantId, companyId);
            if (co == null)
                return ApiResponseDto<CompanyDetailDto>.Fail("Company not found", "ERR_NOT_FOUND");
            return ApiResponseDto<CompanyDetailDto>.Ok(co);
        }

        public ApiResponseDto<Guid> CreateCompany(Guid tenantId, CreateCompanyDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponseDto<Guid>.Fail("Company name is required", "ERR_VALIDATION");

            var settings = _repo.GetSettings(tenantId);
            if (settings != null && _repo.CountActiveCompanies(tenantId) >= settings.MaxCompanies)
                return ApiResponseDto<Guid>.Fail("Company limit reached for your plan", "ERR_LIMIT");

            var id = _repo.CreateCompany(tenantId, dto);
            return ApiResponseDto<Guid>.Ok(id, "Company created successfully");
        }

        public ApiResponseDto<bool> UpdateCompany(Guid tenantId, UpdateCompanyDto dto)
        {
            var co = _repo.GetCompanyById(tenantId, dto.Id);
            if (co == null)
                return ApiResponseDto<bool>.Fail("Company not found", "ERR_NOT_FOUND");

            _repo.UpdateCompany(tenantId, dto);
            return ApiResponseDto<bool>.Ok(true, "Company updated successfully");
        }

        public ApiResponseDto<bool> DeleteCompany(Guid tenantId, Guid companyId)
        {
            var co = _repo.GetCompanyById(tenantId, companyId);
            if (co == null)
                return ApiResponseDto<bool>.Fail("Company not found", "ERR_NOT_FOUND");

            _repo.DeleteCompany(tenantId, companyId);
            return ApiResponseDto<bool>.Ok(true, "Company deleted successfully");
        }

        // ── Branches ─────────────────────────────────────────────────────

        public ApiResponseDto<List<BranchDto>> GetBranches(Guid tenantId, Guid? companyId = null, bool includeInactive = false)
        {
            return ApiResponseDto<List<BranchDto>>.Ok(_repo.GetBranches(tenantId, companyId, includeInactive));
        }

        public ApiResponseDto<BranchDto> GetBranchById(Guid tenantId, Guid branchId)
        {
            var b = _repo.GetBranchById(tenantId, branchId);
            if (b == null)
                return ApiResponseDto<BranchDto>.Fail("Branch not found", "ERR_NOT_FOUND");
            return ApiResponseDto<BranchDto>.Ok(b);
        }

        public ApiResponseDto<Guid> CreateBranch(Guid tenantId, CreateBranchDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponseDto<Guid>.Fail("Branch name is required", "ERR_VALIDATION");
            if (dto.CompanyId == Guid.Empty)
                return ApiResponseDto<Guid>.Fail("Company is required", "ERR_VALIDATION");

            var id = _repo.CreateBranch(tenantId, dto);
            return ApiResponseDto<Guid>.Ok(id, "Branch created successfully");
        }

        public ApiResponseDto<bool> UpdateBranch(Guid tenantId, UpdateBranchDto dto)
        {
            var b = _repo.GetBranchById(tenantId, dto.Id);
            if (b == null)
                return ApiResponseDto<bool>.Fail("Branch not found", "ERR_NOT_FOUND");

            _repo.UpdateBranch(tenantId, dto);
            return ApiResponseDto<bool>.Ok(true, "Branch updated successfully");
        }

        public ApiResponseDto<bool> DeleteBranch(Guid tenantId, Guid branchId)
        {
            var b = _repo.GetBranchById(tenantId, branchId);
            if (b == null)
                return ApiResponseDto<bool>.Fail("Branch not found", "ERR_NOT_FOUND");

            _repo.DeleteBranch(tenantId, branchId);
            return ApiResponseDto<bool>.Ok(true, "Branch deleted successfully");
        }
    }
}
