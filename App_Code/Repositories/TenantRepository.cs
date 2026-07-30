using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using EidUbahle.Domain.DTOs;

namespace EidUbahle.Repositories
{
    /// <summary>
    /// Data access for Tenants, Companies, Branches and tenant subscription info.
    /// </summary>
    public class TenantRepository
    {
        private readonly string _conn;

        public TenantRepository(string connectionString)
        {
            _conn = connectionString;
        }

        // ── Tenant ────────────────────────────────────────────────────────

        public TenantSettingsDto GetSettings(Guid tenantId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    SELECT t.Id, t.Name, t.Subdomain, t.LogoUrl, t.AccentColor, t.ThemeMode,
                           t.ActiveLayout, t.DefaultLanguageCode, t.DefaultCurrencyCode, t.TimeZone,
                           t.MaxUsers, t.MaxCompanies, t.IsActive, t.TrialEndsAt,
                           sp.Name AS PlanName,
                           ts.Status, ts.StartDate, ts.EndDate, sp.MaxUsers, sp.MaxCompanies,
                           sp.MaxBranches, sp.StorageLimitBytes, sp.EnabledModules
                    FROM saas_Tenants t
                    LEFT JOIN saas_TenantSubscriptions ts ON ts.TenantId = t.Id
                    LEFT JOIN saas_SubscriptionPlans sp ON sp.Id = ts.PlanId
                    WHERE t.Id = @Id AND t.IsDeleted = 0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", tenantId);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read())
                        {
                            var dto = new TenantSettingsDto
                            {
                                Id = r.GetGuid(0), Name = r.GetString(1),
                                Subdomain = r.GetString(2),
                                LogoUrl = r.IsDBNull(3) ? null : r.GetString(3),
                                AccentColor = r.IsDBNull(4) ? "#2563EB" : r.GetString(4),
                                ThemeMode = r.GetString(5), ActiveLayout = r.GetString(6),
                                DefaultLanguageCode = r.GetString(7), DefaultCurrencyCode = r.GetString(8),
                                TimeZone = r.GetString(9),
                                MaxUsers = r.GetInt32(10), MaxCompanies = r.GetInt32(11),
                                IsActive = r.GetBoolean(12),
                                TrialEndsAt = r.IsDBNull(13) ? (DateTime?)null : r.GetDateTime(13)
                            };

                            if (!r.IsDBNull(14))
                                dto.Subscription = new SubscriptionInfoDto
                                {
                                    PlanName = r.GetString(14),
                                    Status = r.IsDBNull(15) ? null : r.GetString(15),
                                    StartDate = r.IsDBNull(16) ? DateTime.MinValue : r.GetDateTime(16),
                                    EndDate = r.IsDBNull(17) ? DateTime.MinValue : r.GetDateTime(17),
                                    MaxUsers = r.IsDBNull(18) ? 0 : r.GetInt32(18),
                                    MaxCompanies = r.IsDBNull(19) ? 0 : r.GetInt32(19),
                                    MaxBranches = r.IsDBNull(20) ? 0 : r.GetInt32(20),
                                    StorageLimitBytes = r.IsDBNull(21) ? 0 : r.GetInt64(21),
                                    EnabledModules = r.IsDBNull(22) ? null : r.GetString(22)
                                };
                            return dto;
                        }
                }
            }
            return null;
        }

        public void UpdateSettings(Guid tenantId, UpdateTenantSettingsDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    UPDATE saas_Tenants SET
                        Name=@Name, LogoUrl=@Logo, AccentColor=@Accent, ThemeMode=@Theme,
                        ActiveLayout=@Layout, DefaultLanguageCode=@Lang,
                        DefaultCurrencyCode=@Currency, TimeZone=@TZ, UpdatedAt=GETUTCDATE()
                    WHERE Id=@Id AND IsDeleted=0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", tenantId);
                    cmd.Parameters.AddWithValue("@Name", dto.Name);
                    cmd.Parameters.AddWithValue("@Logo", (object)dto.LogoUrl ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Accent", (object)dto.AccentColor ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Theme", (object)dto.ThemeMode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Layout", (object)dto.ActiveLayout ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Lang", dto.DefaultLanguageCode ?? "en");
                    cmd.Parameters.AddWithValue("@Currency", dto.DefaultCurrencyCode ?? "USD");
                    cmd.Parameters.AddWithValue("@TZ", dto.TimeZone ?? "UTC");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ── Companies ─────────────────────────────────────────────────────

        public List<CompanyListItemDto> GetCompanies(Guid tenantId, bool includeInactive = false)
        {
            var list = new List<CompanyListItemDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var where = "c.TenantId=@T AND c.IsDeleted=0";
                if (!includeInactive) where += " AND c.IsActive=1";
                var sql = $@"
                    SELECT c.Id, c.Name, c.LegalName, c.RegistrationNumber, c.TaxNumber,
                           c.Country, c.City, c.Email, c.Phone, c.LogoUrl,
                           c.DefaultCurrencyCode, c.AccountingBasis, c.IsActive, c.CreatedAt,
                           (SELECT COUNT(1) FROM saas_Branches b WHERE b.CompanyId=c.Id AND b.IsDeleted=0) AS BranchCount
                    FROM saas_Companies c
                    WHERE {where}
                    ORDER BY c.Name";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(MapCompanyListItem(r));
                }
            }
            return list;
        }

        public CompanyDetailDto GetCompanyById(Guid tenantId, Guid companyId)
        {
            CompanyDetailDto dto = null;
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    SELECT c.Id, c.TenantId, c.Name, c.LegalName, c.RegistrationNumber, c.TaxNumber,
                           c.Address, c.City, c.Country, c.Phone, c.Email, c.Website, c.LogoUrl,
                           c.DefaultCurrencyCode, c.FiscalYearStart, c.AccountingBasis,
                           c.IsActive, c.CreatedAt, c.UpdatedAt
                    FROM saas_Companies c
                    WHERE c.Id=@Id AND c.TenantId=@T AND c.IsDeleted=0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", companyId);
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read()) dto = MapCompanyDetail(r);
                }
            }
            if (dto == null) return null;
            dto.Branches = GetBranches(tenantId, companyId, true);
            dto.BranchCount = dto.Branches.Count;
            return dto;
        }

        public int CountActiveCompanies(Guid tenantId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(1) FROM saas_Companies WHERE TenantId=@T AND IsDeleted=0 AND IsActive=1", conn))
                {
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        public Guid CreateCompany(Guid tenantId, CreateCompanyDto dto)
        {
            var id = Guid.NewGuid();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    INSERT INTO saas_Companies(Id,TenantId,Name,LegalName,RegistrationNumber,TaxNumber,
                        Address,City,Country,Phone,Email,Website,DefaultCurrencyCode,
                        FiscalYearStart,AccountingBasis,IsActive,IsDeleted,CreatedAt,UpdatedAt)
                    VALUES(@Id,@T,@Name,@Legal,@Reg,@Tax,@Addr,@City,@Country,@Phone,@Email,@Web,
                        @Curr,@FY,@Basis,1,0,GETUTCDATE(),GETUTCDATE())";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    cmd.Parameters.AddWithValue("@Name", dto.Name);
                    cmd.Parameters.AddWithValue("@Legal", (object)dto.LegalName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Reg", (object)dto.RegistrationNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Tax", (object)dto.TaxNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Addr", (object)dto.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@City", (object)dto.City ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Country", (object)dto.Country ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Phone", (object)dto.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object)dto.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Web", (object)dto.Website ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Curr", dto.DefaultCurrencyCode ?? "USD");
                    cmd.Parameters.AddWithValue("@FY", dto.FiscalYearStart ?? "01-01");
                    cmd.Parameters.AddWithValue("@Basis", dto.AccountingBasis ?? "Accrual");
                    cmd.ExecuteNonQuery();
                }
            }
            return id;
        }

        public void UpdateCompany(Guid tenantId, UpdateCompanyDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    UPDATE saas_Companies SET
                        Name=@Name, LegalName=@Legal, RegistrationNumber=@Reg, TaxNumber=@Tax,
                        Address=@Addr, City=@City, Country=@Country, Phone=@Phone, Email=@Email,
                        Website=@Web, LogoUrl=@Logo, DefaultCurrencyCode=@Curr,
                        FiscalYearStart=@FY, AccountingBasis=@Basis, IsActive=@Active,
                        UpdatedAt=GETUTCDATE()
                    WHERE Id=@Id AND TenantId=@T AND IsDeleted=0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", dto.Id);
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    cmd.Parameters.AddWithValue("@Name", dto.Name);
                    cmd.Parameters.AddWithValue("@Legal", (object)dto.LegalName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Reg", (object)dto.RegistrationNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Tax", (object)dto.TaxNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Addr", (object)dto.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@City", (object)dto.City ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Country", (object)dto.Country ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Phone", (object)dto.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object)dto.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Web", (object)dto.Website ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Logo", (object)dto.LogoUrl ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Curr", dto.DefaultCurrencyCode ?? "USD");
                    cmd.Parameters.AddWithValue("@FY", dto.FiscalYearStart ?? "01-01");
                    cmd.Parameters.AddWithValue("@Basis", dto.AccountingBasis ?? "Accrual");
                    cmd.Parameters.AddWithValue("@Active", dto.IsActive);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteCompany(Guid tenantId, Guid companyId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "UPDATE saas_Companies SET IsDeleted=1, IsActive=0, UpdatedAt=GETUTCDATE() WHERE Id=@Id AND TenantId=@T", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", companyId);
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ── Branches ─────────────────────────────────────────────────────

        public List<BranchDto> GetBranches(Guid tenantId, Guid? companyId = null, bool includeInactive = false)
        {
            var list = new List<BranchDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var where = "b.TenantId=@T AND b.IsDeleted=0";
                if (companyId.HasValue) where += " AND b.CompanyId=@CId";
                if (!includeInactive) where += " AND b.IsActive=1";
                var sql = $@"
                    SELECT b.Id, b.CompanyId, c.Name AS CompanyName, b.Name, b.Code,
                           b.Address, b.Phone, b.IsHeadOffice, b.IsActive, b.CreatedAt,
                           (SELECT COUNT(1) FROM sys_UserCompanyBranches ucb WHERE ucb.BranchId=b.Id) AS UserCount
                    FROM saas_Branches b
                    JOIN saas_Companies c ON c.Id=b.CompanyId
                    WHERE {where}
                    ORDER BY c.Name, b.IsHeadOffice DESC, b.Name";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    if (companyId.HasValue) cmd.Parameters.AddWithValue("@CId", companyId.Value);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapBranch(r));
                }
            }
            return list;
        }

        public BranchDto GetBranchById(Guid tenantId, Guid branchId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    SELECT b.Id, b.CompanyId, c.Name AS CompanyName, b.Name, b.Code,
                           b.Address, b.Phone, b.IsHeadOffice, b.IsActive, b.CreatedAt,
                           (SELECT COUNT(1) FROM sys_UserCompanyBranches ucb WHERE ucb.BranchId=b.Id)
                    FROM saas_Branches b
                    JOIN saas_Companies c ON c.Id=b.CompanyId
                    WHERE b.Id=@Id AND b.TenantId=@T AND b.IsDeleted=0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", branchId);
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    using (var r = cmd.ExecuteReader())
                        return r.Read() ? MapBranch(r) : null;
                }
            }
        }

        public Guid CreateBranch(Guid tenantId, CreateBranchDto dto)
        {
            var id = Guid.NewGuid();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();

                // enforce single head-office per company
                if (dto.IsHeadOffice)
                    ClearHeadOffice(conn, null, dto.CompanyId);

                const string sql = @"
                    INSERT INTO saas_Branches(Id,TenantId,CompanyId,Name,Code,Address,Phone,
                        IsHeadOffice,IsActive,IsDeleted,CreatedAt,UpdatedAt)
                    VALUES(@Id,@T,@C,@Name,@Code,@Addr,@Phone,@HO,1,0,GETUTCDATE(),GETUTCDATE())";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    cmd.Parameters.AddWithValue("@C", dto.CompanyId);
                    cmd.Parameters.AddWithValue("@Name", dto.Name);
                    cmd.Parameters.AddWithValue("@Code", (object)dto.Code ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Addr", (object)dto.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Phone", (object)dto.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@HO", dto.IsHeadOffice);
                    cmd.ExecuteNonQuery();
                }
            }
            return id;
        }

        public void UpdateBranch(Guid tenantId, UpdateBranchDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    if (dto.IsHeadOffice)
                        ClearHeadOffice(conn, tx, dto.CompanyId);

                    const string sql = @"
                        UPDATE saas_Branches SET
                            Name=@Name, Code=@Code, Address=@Addr, Phone=@Phone,
                            IsHeadOffice=@HO, IsActive=@Active, UpdatedAt=GETUTCDATE()
                        WHERE Id=@Id AND TenantId=@T AND IsDeleted=0";
                    using (var cmd = new SqlCommand(sql, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Id", dto.Id);
                        cmd.Parameters.AddWithValue("@T", tenantId);
                        cmd.Parameters.AddWithValue("@Name", dto.Name);
                        cmd.Parameters.AddWithValue("@Code", (object)dto.Code ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Addr", (object)dto.Address ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Phone", (object)dto.Phone ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@HO", dto.IsHeadOffice);
                        cmd.Parameters.AddWithValue("@Active", dto.IsActive);
                        cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
            }
        }

        public void DeleteBranch(Guid tenantId, Guid branchId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "UPDATE saas_Branches SET IsDeleted=1, IsActive=0, UpdatedAt=GETUTCDATE() WHERE Id=@Id AND TenantId=@T", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", branchId);
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ── Onboarding status ────────────────────────────────────────────

        public OnboardingStatusDto GetOnboardingStatus(Guid tenantId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var status = new OnboardingStatusDto();

                using (var cmd = new SqlCommand(
                    "SELECT COUNT(1) FROM saas_Companies WHERE TenantId=@T AND IsDeleted=0", conn))
                {
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    status.HasCompany = (int)cmd.ExecuteScalar() > 0;
                }
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(1) FROM saas_Branches b JOIN saas_Companies c ON c.Id=b.CompanyId WHERE b.TenantId=@T AND b.IsDeleted=0", conn))
                {
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    status.HasBranch = (int)cmd.ExecuteScalar() > 0;
                }
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(1) FROM sys_Users WHERE TenantId=@T AND IsDeleted=0 AND IsTenantAdmin=0 AND IsActive=1", conn))
                {
                    cmd.Parameters.AddWithValue("@T", tenantId);
                    status.HasNonAdminUser = (int)cmd.ExecuteScalar() > 0;
                }

                status.IsComplete = status.HasCompany && status.HasBranch;
                status.CurrentStep = !status.HasCompany ? 1
                                   : !status.HasBranch ? 2
                                   : !status.HasNonAdminUser ? 3 : 4;
                return status;
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private void ClearHeadOffice(SqlConnection conn, SqlTransaction tx, Guid companyId)
        {
            var sql = "UPDATE saas_Branches SET IsHeadOffice=0 WHERE CompanyId=@C";
            using (var cmd = tx != null
                ? new SqlCommand(sql, conn, tx)
                : new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@C", companyId);
                cmd.ExecuteNonQuery();
            }
        }

        private static CompanyListItemDto MapCompanyListItem(SqlDataReader r) => new CompanyListItemDto
        {
            Id = r.GetGuid(0), Name = r.GetString(1),
            LegalName = r.IsDBNull(2) ? null : r.GetString(2),
            RegistrationNumber = r.IsDBNull(3) ? null : r.GetString(3),
            TaxNumber = r.IsDBNull(4) ? null : r.GetString(4),
            Country = r.IsDBNull(5) ? null : r.GetString(5),
            City = r.IsDBNull(6) ? null : r.GetString(6),
            Email = r.IsDBNull(7) ? null : r.GetString(7),
            Phone = r.IsDBNull(8) ? null : r.GetString(8),
            LogoUrl = r.IsDBNull(9) ? null : r.GetString(9),
            DefaultCurrencyCode = r.GetString(10),
            AccountingBasis = r.GetString(11), IsActive = r.GetBoolean(12),
            CreatedAt = r.GetDateTime(13), BranchCount = r.GetInt32(14)
        };

        private static CompanyDetailDto MapCompanyDetail(SqlDataReader r) => new CompanyDetailDto
        {
            Id = r.GetGuid(0), TenantId = r.GetGuid(1), Name = r.GetString(2),
            LegalName = r.IsDBNull(3) ? null : r.GetString(3),
            RegistrationNumber = r.IsDBNull(4) ? null : r.GetString(4),
            TaxNumber = r.IsDBNull(5) ? null : r.GetString(5),
            Address = r.IsDBNull(6) ? null : r.GetString(6),
            City = r.IsDBNull(7) ? null : r.GetString(7),
            Country = r.IsDBNull(8) ? null : r.GetString(8),
            Phone = r.IsDBNull(9) ? null : r.GetString(9),
            Email = r.IsDBNull(10) ? null : r.GetString(10),
            Website = r.IsDBNull(11) ? null : r.GetString(11),
            LogoUrl = r.IsDBNull(12) ? null : r.GetString(12),
            DefaultCurrencyCode = r.GetString(13),
            FiscalYearStart = r.GetString(14),
            AccountingBasis = r.GetString(15), IsActive = r.GetBoolean(16),
            CreatedAt = r.GetDateTime(17), UpdatedAt = r.GetDateTime(18)
        };

        private static BranchDto MapBranch(SqlDataReader r) => new BranchDto
        {
            Id = r.GetGuid(0), CompanyId = r.GetGuid(1),
            CompanyName = r.GetString(2), Name = r.GetString(3),
            Code = r.IsDBNull(4) ? null : r.GetString(4),
            Address = r.IsDBNull(5) ? null : r.GetString(5),
            Phone = r.IsDBNull(6) ? null : r.GetString(6),
            IsHeadOffice = r.GetBoolean(7), IsActive = r.GetBoolean(8),
            CreatedAt = r.GetDateTime(9), UserCount = r.GetInt32(10)
        };
    }
}
