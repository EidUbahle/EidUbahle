using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using EidUbahle.Domain.DTOs;

namespace EidUbahle.Repositories
{
    /// <summary>
    /// Data access for the Core Accounting module:
    /// Currencies, Exchange Rates, Fiscal Years/Periods, Chart of Accounts,
    /// Journal Entries, Bank Accounts, Bank Reconciliations.
    /// All queries are tenant + company scoped.
    /// </summary>
    public class AccountingRepository
    {
        private readonly string _conn;

        public AccountingRepository(string connectionString)
        {
            _conn = connectionString;
        }

        // ═══════════════════════════════════════════════════════════════════
        // CURRENCIES
        // ═══════════════════════════════════════════════════════════════════

        public List<CurrencyDto> GetCurrencies(bool activeOnly = true)
        {
            var list = new List<CurrencyDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var sql = "SELECT Code,Name,Symbol,DecimalPlaces,IsBaseCurrency,IsActive FROM acc_Currencies";
                if (activeOnly) sql += " WHERE IsActive=1";
                sql += " ORDER BY IsBaseCurrency DESC, Code";
                using (var cmd = new SqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) list.Add(MapCurrency(r));
            }
            return list;
        }

        public CurrencyDto GetCurrency(string code)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = "SELECT Code,Name,Symbol,DecimalPlaces,IsBaseCurrency,IsActive FROM acc_Currencies WHERE Code=@Code";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Code", code);
                    using (var r = cmd.ExecuteReader())
                        return r.Read() ? MapCurrency(r) : null;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // EXCHANGE RATES
        // ═══════════════════════════════════════════════════════════════════

        public List<ExchangeRateDto> GetExchangeRates(Guid tenantId, string fromCurrency = null, int limit = 50)
        {
            var list = new List<ExchangeRateDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var sql = @"SELECT TOP(@Limit) Id,FromCurrency,ToCurrency,Rate,EffectiveDate,Source,CreatedAt
                            FROM acc_ExchangeRates WHERE TenantId=@TenantId";
                if (!string.IsNullOrEmpty(fromCurrency)) sql += " AND FromCurrency=@From";
                sql += " ORDER BY EffectiveDate DESC, CreatedAt DESC";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Limit", limit);
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    if (!string.IsNullOrEmpty(fromCurrency))
                        cmd.Parameters.AddWithValue("@From", fromCurrency);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapExchangeRate(r));
                }
            }
            return list;
        }

        public decimal GetExchangeRate(Guid tenantId, string fromCurrency, string toCurrency, DateTime date)
        {
            if (fromCurrency == toCurrency) return 1m;
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"SELECT TOP 1 Rate FROM acc_ExchangeRates
                                     WHERE TenantId=@TenantId AND FromCurrency=@From AND ToCurrency=@To
                                       AND EffectiveDate <= @Date
                                     ORDER BY EffectiveDate DESC";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    cmd.Parameters.AddWithValue("@From", fromCurrency);
                    cmd.Parameters.AddWithValue("@To", toCurrency);
                    cmd.Parameters.AddWithValue("@Date", date.Date);
                    var result = cmd.ExecuteScalar();
                    return result != null && result != DBNull.Value ? (decimal)result : 1m;
                }
            }
        }

        public Guid CreateExchangeRate(Guid tenantId, Guid userId, CreateExchangeRateDto dto)
        {
            var id = Guid.NewGuid();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"INSERT INTO acc_ExchangeRates(Id,TenantId,FromCurrency,ToCurrency,Rate,EffectiveDate,Source,CreatedAt,CreatedBy)
                                     VALUES(@Id,@TenantId,@From,@To,@Rate,@Date,@Source,GETUTCDATE(),@UserId)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    cmd.Parameters.AddWithValue("@From", dto.FromCurrency);
                    cmd.Parameters.AddWithValue("@To", dto.ToCurrency);
                    cmd.Parameters.AddWithValue("@Rate", dto.Rate);
                    cmd.Parameters.AddWithValue("@Date", dto.EffectiveDate.Date);
                    cmd.Parameters.AddWithValue("@Source", (object)dto.Source ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
            return id;
        }

        // ═══════════════════════════════════════════════════════════════════
        // FISCAL YEARS
        // ═══════════════════════════════════════════════════════════════════

        public List<FiscalYearDto> GetFiscalYears(Guid tenantId, Guid companyId)
        {
            var list = new List<FiscalYearDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    SELECT fy.Id, fy.CompanyId, c.Name AS CompanyName, fy.Year,
                           fy.StartDate, fy.EndDate, fy.Status, fy.ClosedAt,
                           u.FullName AS ClosedByName, fy.CreatedAt
                    FROM acc_FiscalYears fy
                    JOIN saas_Companies c ON c.Id = fy.CompanyId
                    LEFT JOIN sys_Users u ON u.Id = fy.ClosedBy
                    WHERE fy.TenantId=@TenantId AND fy.CompanyId=@CompanyId
                    ORDER BY fy.Year DESC";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    cmd.Parameters.AddWithValue("@CompanyId", companyId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapFiscalYear(r));
                }
            }
            foreach (var fy in list)
            {
                fy.Periods = GetFiscalPeriods(fy.Id);
                fy.OpenPeriodCount = fy.Periods.FindAll(p => p.Status == "Open").Count;
                fy.ClosedPeriodCount = fy.Periods.FindAll(p => p.Status != "Open").Count;
            }
            return list;
        }

        public FiscalYearDto GetFiscalYear(Guid fiscalYearId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    SELECT fy.Id, fy.CompanyId, c.Name, fy.Year, fy.StartDate, fy.EndDate,
                           fy.Status, fy.ClosedAt, u.FullName, fy.CreatedAt
                    FROM acc_FiscalYears fy
                    JOIN saas_Companies c ON c.Id = fy.CompanyId
                    LEFT JOIN sys_Users u ON u.Id = fy.ClosedBy
                    WHERE fy.Id=@Id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", fiscalYearId);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        var fy = MapFiscalYear(r);
                        r.Close();
                        fy.Periods = GetFiscalPeriods(fiscalYearId);
                        return fy;
                    }
                }
            }
        }

        public List<FiscalPeriodDto> GetFiscalPeriods(Guid fiscalYearId)
        {
            var list = new List<FiscalPeriodDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"SELECT Id,FiscalYearId,CompanyId,PeriodNumber,Name,StartDate,EndDate,Status,ClosedAt
                                     FROM acc_FiscalPeriods WHERE FiscalYearId=@FiscalYearId ORDER BY PeriodNumber";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@FiscalYearId", fiscalYearId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapFiscalPeriod(r));
                }
            }
            return list;
        }

        public FiscalPeriodDto GetOpenPeriodForDate(Guid companyId, DateTime date)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"SELECT fp.Id, fp.FiscalYearId, fp.CompanyId, fp.PeriodNumber, fp.Name,
                                            fp.StartDate, fp.EndDate, fp.Status, fp.ClosedAt
                                     FROM acc_FiscalPeriods fp
                                     WHERE fp.CompanyId=@CompanyId AND fp.Status='Open'
                                       AND fp.StartDate <= @Date AND fp.EndDate >= @Date";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@CompanyId", companyId);
                    cmd.Parameters.AddWithValue("@Date", date.Date);
                    using (var r = cmd.ExecuteReader())
                        return r.Read() ? MapFiscalPeriod(r) : null;
                }
            }
        }

        public Guid CreateFiscalYear(Guid tenantId, Guid userId, CreateFiscalYearDto dto)
        {
            var id = Guid.NewGuid();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    const string sql = @"INSERT INTO acc_FiscalYears(Id,TenantId,CompanyId,Year,StartDate,EndDate,Status,CreatedAt,UpdatedAt,CreatedBy)
                                         VALUES(@Id,@TenantId,@CompanyId,@Year,@Start,@End,'Open',GETUTCDATE(),GETUTCDATE(),@UserId)";
                    using (var cmd = new SqlCommand(sql, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@TenantId", tenantId);
                        cmd.Parameters.AddWithValue("@CompanyId", dto.CompanyId);
                        cmd.Parameters.AddWithValue("@Year", dto.Year);
                        cmd.Parameters.AddWithValue("@Start", dto.StartDate.Date);
                        cmd.Parameters.AddWithValue("@End", dto.EndDate.Date);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }

                    // Auto-generate 12 monthly periods
                    var current = dto.StartDate;
                    for (int i = 1; i <= 12; i++)
                    {
                        var periodStart = new DateTime(current.Year, current.Month, 1);
                        var periodEnd   = periodStart.AddMonths(1).AddDays(-1);
                        if (periodEnd > dto.EndDate) periodEnd = dto.EndDate;

                        const string pSql = @"INSERT INTO acc_FiscalPeriods(Id,TenantId,CompanyId,FiscalYearId,PeriodNumber,Name,StartDate,EndDate,Status,CreatedAt)
                                              VALUES(@Id,@TenantId,@CompanyId,@FiscalYearId,@Num,@Name,@Start,@End,'Open',GETUTCDATE())";
                        using (var pcmd = new SqlCommand(pSql, conn, tran))
                        {
                            pcmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
                            pcmd.Parameters.AddWithValue("@TenantId", tenantId);
                            pcmd.Parameters.AddWithValue("@CompanyId", dto.CompanyId);
                            pcmd.Parameters.AddWithValue("@FiscalYearId", id);
                            pcmd.Parameters.AddWithValue("@Num", i);
                            pcmd.Parameters.AddWithValue("@Name", periodStart.ToString("MMM yyyy"));
                            pcmd.Parameters.AddWithValue("@Start", periodStart);
                            pcmd.Parameters.AddWithValue("@End", periodEnd);
                            pcmd.ExecuteNonQuery();
                        }

                        current = current.AddMonths(1);
                        if (current > dto.EndDate) break;
                    }

                    tran.Commit();
                }
            }
            return id;
        }

        public bool FiscalYearExists(Guid companyId, int year)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM acc_FiscalYears WHERE CompanyId=@C AND Year=@Y", conn))
                {
                    cmd.Parameters.AddWithValue("@C", companyId);
                    cmd.Parameters.AddWithValue("@Y", year);
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        public void CloseFiscalPeriod(Guid fiscalPeriodId, Guid userId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand("UPDATE acc_FiscalPeriods SET Status='Closed',ClosedAt=GETUTCDATE(),ClosedBy=@UserId WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", fiscalPeriodId);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void CloseFiscalYear(Guid fiscalYearId, Guid userId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    using (var cmd = new SqlCommand("UPDATE acc_FiscalPeriods SET Status='Closed',ClosedAt=GETUTCDATE(),ClosedBy=@UserId WHERE FiscalYearId=@Id AND Status='Open'", conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@Id", fiscalYearId);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new SqlCommand("UPDATE acc_FiscalYears SET Status='Closed',ClosedAt=GETUTCDATE(),ClosedBy=@UserId,UpdatedAt=GETUTCDATE() WHERE Id=@Id", conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@Id", fiscalYearId);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }
                    tran.Commit();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // CHART OF ACCOUNTS
        // ═══════════════════════════════════════════════════════════════════

        public List<AccountListItemDto> GetAccounts(Guid companyId, bool activeOnly = false, string accountType = null)
        {
            var list = new List<AccountListItemDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var where = "CompanyId=@CompanyId AND IsDeleted=0";
                if (activeOnly) where += " AND IsActive=1";
                if (!string.IsNullOrEmpty(accountType)) where += " AND AccountType=@Type";
                var sql = $"SELECT Id,ParentId,Code,Name,AccountType,AccountSubType,NormalBalance,CurrencyCode,IsPosting,IsActive,Level,SortOrder FROM acc_Accounts WHERE {where} ORDER BY Code";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@CompanyId", companyId);
                    if (!string.IsNullOrEmpty(accountType))
                        cmd.Parameters.AddWithValue("@Type", accountType);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapAccountListItem(r));
                }
            }
            return list;
        }

        public AccountDto GetAccountById(Guid accountId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"SELECT a.Id, a.ParentId, p.Name AS ParentName, a.Code, a.Name,
                                            a.AccountType, a.AccountSubType, a.NormalBalance, a.CurrencyCode,
                                            a.IsPosting, a.IsActive, a.Description, a.Level, a.SortOrder, a.CreatedAt
                                     FROM acc_Accounts a
                                     LEFT JOIN acc_Accounts p ON p.Id = a.ParentId
                                     WHERE a.Id=@Id AND a.IsDeleted=0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", accountId);
                    using (var r = cmd.ExecuteReader())
                        return r.Read() ? MapAccountDetail(r) : null;
                }
            }
        }

        public bool AccountCodeExists(Guid companyId, string code, Guid? excludeId = null)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var sql = "SELECT COUNT(*) FROM acc_Accounts WHERE CompanyId=@C AND Code=@Code AND IsDeleted=0";
                if (excludeId.HasValue) sql += " AND Id<>@ExcludeId";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@C", companyId);
                    cmd.Parameters.AddWithValue("@Code", code);
                    if (excludeId.HasValue) cmd.Parameters.AddWithValue("@ExcludeId", excludeId.Value);
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        public Guid CreateAccount(Guid tenantId, Guid userId, CreateAccountDto dto)
        {
            var id = Guid.NewGuid();
            int level = 1;
            string normalBalance = GetDefaultNormalBalance(dto.AccountType);

            if (dto.ParentId.HasValue)
            {
                using (var conn2 = new SqlConnection(_conn))
                {
                    conn2.Open();
                    using (var c2 = new SqlCommand("SELECT Level FROM acc_Accounts WHERE Id=@Id", conn2))
                    {
                        c2.Parameters.AddWithValue("@Id", dto.ParentId.Value);
                        var lv = c2.ExecuteScalar();
                        if (lv != null && lv != DBNull.Value) level = (int)lv + 1;
                    }
                }
            }

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"INSERT INTO acc_Accounts(Id,TenantId,CompanyId,ParentId,Code,Name,AccountType,AccountSubType,
                                      NormalBalance,CurrencyCode,IsPosting,IsActive,Description,Level,SortOrder,CreatedAt,UpdatedAt,IsDeleted,CreatedBy)
                                     VALUES(@Id,@TenantId,@CompanyId,@ParentId,@Code,@Name,@AccountType,@AccountSubType,
                                            @NormalBalance,@CurrencyCode,@IsPosting,1,@Description,@Level,@SortOrder,GETUTCDATE(),GETUTCDATE(),0,@UserId)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    cmd.Parameters.AddWithValue("@CompanyId", dto.CompanyId);
                    cmd.Parameters.AddWithValue("@ParentId", (object)dto.ParentId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Code", dto.Code);
                    cmd.Parameters.AddWithValue("@Name", dto.Name);
                    cmd.Parameters.AddWithValue("@AccountType", dto.AccountType);
                    cmd.Parameters.AddWithValue("@AccountSubType", (object)dto.AccountSubType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NormalBalance", normalBalance);
                    cmd.Parameters.AddWithValue("@CurrencyCode", (object)dto.CurrencyCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsPosting", dto.IsPosting);
                    cmd.Parameters.AddWithValue("@Description", (object)dto.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Level", level);
                    cmd.Parameters.AddWithValue("@SortOrder", dto.SortOrder);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
            return id;
        }

        public bool UpdateAccount(Guid tenantId, Guid userId, UpdateAccountDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"UPDATE acc_Accounts
                                     SET Name=@Name, AccountSubType=@AccountSubType, CurrencyCode=@CurrencyCode,
                                         IsPosting=@IsPosting, IsActive=@IsActive, Description=@Description,
                                         SortOrder=@SortOrder, UpdatedAt=GETUTCDATE(), UpdatedBy=@UserId
                                     WHERE Id=@Id AND CompanyId=@CompanyId AND IsDeleted=0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", dto.Id);
                    cmd.Parameters.AddWithValue("@CompanyId", dto.CompanyId);
                    cmd.Parameters.AddWithValue("@Name", dto.Name);
                    cmd.Parameters.AddWithValue("@AccountSubType", (object)dto.AccountSubType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CurrencyCode", (object)dto.CurrencyCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsPosting", dto.IsPosting);
                    cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);
                    cmd.Parameters.AddWithValue("@Description", (object)dto.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SortOrder", dto.SortOrder);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool DeleteAccount(Guid accountId, Guid userId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                // Safety: cannot delete if it has posted journal lines
                using (var check = new SqlCommand(@"SELECT COUNT(*) FROM acc_JournalEntryLines jl
                                                    JOIN acc_JournalEntries je ON je.Id=jl.JournalEntryId
                                                    WHERE jl.AccountId=@Id AND je.Status='Posted'", conn))
                {
                    check.Parameters.AddWithValue("@Id", accountId);
                    if ((int)check.ExecuteScalar() > 0) return false;
                }
                using (var cmd = new SqlCommand("UPDATE acc_Accounts SET IsDeleted=1,DeletedAt=GETUTCDATE(),UpdatedBy=@UserId WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", accountId);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool AccountHasTransactions(Guid accountId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM acc_JournalEntryLines WHERE AccountId=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", accountId);
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // JOURNAL ENTRIES
        // ═══════════════════════════════════════════════════════════════════

        public PagedResultDto<JournalEntryListItemDto> GetJournalEntries(
            Guid tenantId, Guid companyId, string search, string status,
            DateTime? startDate, DateTime? endDate, int page, int pageSize)
        {
            var result = new PagedResultDto<JournalEntryListItemDto> { Page = page, PageSize = pageSize };
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var where = "je.CompanyId=@CompanyId AND je.IsDeleted=0";
                if (!string.IsNullOrWhiteSpace(search))
                    where += " AND (je.EntryNumber LIKE @Search OR je.Description LIKE @Search OR je.Reference LIKE @Search)";
                if (!string.IsNullOrWhiteSpace(status))
                    where += " AND je.Status=@Status";
                if (startDate.HasValue)
                    where += " AND je.EntryDate >= @StartDate";
                if (endDate.HasValue)
                    where += " AND je.EntryDate <= @EndDate";

                var countSql = $"SELECT COUNT(*) FROM acc_JournalEntries je WHERE {where}";
                using (var cmd = new SqlCommand(countSql, conn))
                {
                    AddJEParams(cmd, companyId, search, status, startDate, endDate);
                    result.TotalCount = (int)cmd.ExecuteScalar();
                }

                var listSql = $@"
                    SELECT je.Id, je.EntryNumber, je.EntryDate, je.Description, je.Reference,
                           je.EntryType, je.CurrencyCode, je.ExchangeRate, je.TotalDebit, je.TotalCredit,
                           je.Status, u1.FullName AS CreatedByName, u2.FullName AS PostedByName,
                           je.PostedAt, je.CreatedAt,
                           (SELECT COUNT(*) FROM acc_JournalEntryLines WHERE JournalEntryId=je.Id) AS LineCount
                    FROM acc_JournalEntries je
                    LEFT JOIN sys_Users u1 ON u1.Id = je.CreatedBy
                    LEFT JOIN sys_Users u2 ON u2.Id = je.PostedBy
                    WHERE {where}
                    ORDER BY je.EntryDate DESC, je.EntryNumber DESC
                    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
                using (var cmd = new SqlCommand(listSql, conn))
                {
                    AddJEParams(cmd, companyId, search, status, startDate, endDate);
                    cmd.Parameters.AddWithValue("@Skip", (page - 1) * pageSize);
                    cmd.Parameters.AddWithValue("@Take", pageSize);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) result.Items.Add(MapJEListItem(r));
                }
            }
            return result;
        }

        public JournalEntryDetailDto GetJournalEntry(Guid journalEntryId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"
                    SELECT je.Id, je.EntryNumber, je.EntryDate, je.Description, je.Reference, je.EntryType,
                           je.CurrencyCode, je.ExchangeRate, je.TotalDebit, je.TotalCredit,
                           je.BaseCurrencyDebit, je.BaseCurrencyCredit, je.Status,
                           u1.FullName AS CreatedByName, u2.FullName AS PostedByName,
                           je.PostedAt, je.CreatedAt, je.Notes, je.ReversedEntryId,
                           rev.EntryNumber AS ReversedEntryNumber,
                           je.CompanyId, c.Name AS CompanyName, je.BranchId, b.Name AS BranchName,
                           je.FiscalPeriodId, fp.Name AS FiscalPeriodName,
                           (SELECT COUNT(*) FROM acc_JournalEntryLines WHERE JournalEntryId=je.Id) AS LineCount
                    FROM acc_JournalEntries je
                    LEFT JOIN sys_Users u1 ON u1.Id = je.CreatedBy
                    LEFT JOIN sys_Users u2 ON u2.Id = je.PostedBy
                    LEFT JOIN acc_JournalEntries rev ON rev.Id = je.ReversedEntryId
                    LEFT JOIN saas_Companies c ON c.Id = je.CompanyId
                    LEFT JOIN saas_Branches b ON b.Id = je.BranchId
                    LEFT JOIN acc_FiscalPeriods fp ON fp.Id = je.FiscalPeriodId
                    WHERE je.Id=@Id AND je.IsDeleted=0";
                JournalEntryDetailDto dto = null;
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", journalEntryId);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read()) dto = MapJEDetail(r);
                    }
                }
                if (dto == null) return null;

                // Load lines
                const string linesSql = @"
                    SELECT jl.Id, jl.LineNumber, jl.AccountId, a.Code AS AccountCode, a.Name AS AccountName,
                           a.AccountType, jl.Description, jl.DebitAmount, jl.CreditAmount,
                           jl.CurrencyCode, jl.ExchangeRate, jl.BaseDebit, jl.BaseCredit, jl.Reference
                    FROM acc_JournalEntryLines jl
                    JOIN acc_Accounts a ON a.Id = jl.AccountId
                    WHERE jl.JournalEntryId=@Id
                    ORDER BY jl.LineNumber";
                using (var cmd = new SqlCommand(linesSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", journalEntryId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) dto.Lines.Add(MapJELine(r));
                }
                return dto;
            }
        }

        public string GenerateEntryNumber(Guid companyId, DateTime entryDate)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand("EXEC sp_GenerateJournalEntryNumber @CompanyId, @EntryDate", conn))
                {
                    cmd.Parameters.AddWithValue("@CompanyId", companyId);
                    cmd.Parameters.AddWithValue("@EntryDate", entryDate.Date);
                    return (string)cmd.ExecuteScalar();
                }
            }
        }

        public Guid CreateJournalEntry(Guid tenantId, Guid userId, CreateJournalEntryDto dto, Guid fiscalPeriodId, string entryNumber)
        {
            var id = Guid.NewGuid();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    const string sql = @"
                        INSERT INTO acc_JournalEntries(Id,TenantId,CompanyId,BranchId,FiscalPeriodId,EntryNumber,EntryDate,
                                Description,Reference,EntryType,CurrencyCode,ExchangeRate,Status,Notes,
                                CreatedAt,UpdatedAt,IsDeleted,CreatedBy)
                        VALUES(@Id,@TenantId,@CompanyId,@BranchId,@PeriodId,@EntryNumber,@EntryDate,
                               @Description,@Reference,@EntryType,@CurrencyCode,@ExchangeRate,'Draft',@Notes,
                               GETUTCDATE(),GETUTCDATE(),0,@UserId)";
                    using (var cmd = new SqlCommand(sql, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@TenantId", tenantId);
                        cmd.Parameters.AddWithValue("@CompanyId", dto.CompanyId);
                        cmd.Parameters.AddWithValue("@BranchId", (object)dto.BranchId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PeriodId", fiscalPeriodId);
                        cmd.Parameters.AddWithValue("@EntryNumber", entryNumber);
                        cmd.Parameters.AddWithValue("@EntryDate", dto.EntryDate.Date);
                        cmd.Parameters.AddWithValue("@Description", dto.Description);
                        cmd.Parameters.AddWithValue("@Reference", (object)dto.Reference ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EntryType", dto.EntryType ?? "Normal");
                        cmd.Parameters.AddWithValue("@CurrencyCode", dto.CurrencyCode ?? "USD");
                        cmd.Parameters.AddWithValue("@ExchangeRate", dto.ExchangeRate <= 0 ? 1m : dto.ExchangeRate);
                        cmd.Parameters.AddWithValue("@Notes", (object)dto.Notes ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }

                    int lineNum = 1;
                    foreach (var line in dto.Lines)
                    {
                        decimal baseDebit  = line.DebitAmount  * (line.ExchangeRate > 0 ? line.ExchangeRate : dto.ExchangeRate);
                        decimal baseCredit = line.CreditAmount * (line.ExchangeRate > 0 ? line.ExchangeRate : dto.ExchangeRate);
                        const string lSql = @"INSERT INTO acc_JournalEntryLines(Id,JournalEntryId,TenantId,CompanyId,AccountId,LineNumber,
                                                Description,DebitAmount,CreditAmount,CurrencyCode,ExchangeRate,BaseDebit,BaseCredit,Reference,CreatedAt)
                                             VALUES(@Id,@JEId,@TenantId,@CompanyId,@AccountId,@LineNum,
                                                    @Desc,@Debit,@Credit,@Currency,@Rate,@BaseDebit,@BaseCredit,@Ref,GETUTCDATE())";
                        using (var lcmd = new SqlCommand(lSql, conn, tran))
                        {
                            lcmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
                            lcmd.Parameters.AddWithValue("@JEId", id);
                            lcmd.Parameters.AddWithValue("@TenantId", tenantId);
                            lcmd.Parameters.AddWithValue("@CompanyId", dto.CompanyId);
                            lcmd.Parameters.AddWithValue("@AccountId", line.AccountId);
                            lcmd.Parameters.AddWithValue("@LineNum", lineNum++);
                            lcmd.Parameters.AddWithValue("@Desc", (object)line.Description ?? DBNull.Value);
                            lcmd.Parameters.AddWithValue("@Debit", line.DebitAmount);
                            lcmd.Parameters.AddWithValue("@Credit", line.CreditAmount);
                            lcmd.Parameters.AddWithValue("@Currency", line.CurrencyCode ?? dto.CurrencyCode ?? "USD");
                            lcmd.Parameters.AddWithValue("@Rate", line.ExchangeRate > 0 ? line.ExchangeRate : dto.ExchangeRate);
                            lcmd.Parameters.AddWithValue("@BaseDebit", baseDebit);
                            lcmd.Parameters.AddWithValue("@BaseCredit", baseCredit);
                            lcmd.Parameters.AddWithValue("@Ref", (object)line.Reference ?? DBNull.Value);
                            lcmd.ExecuteNonQuery();
                        }
                    }

                    tran.Commit();
                }
            }
            return id;
        }

        public (bool Success, string ErrorMessage) PostJournalEntry(Guid journalEntryId, Guid userId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand("EXEC sp_PostJournalEntry @JournalEntryId, @PostedBy", conn))
                {
                    cmd.Parameters.AddWithValue("@JournalEntryId", journalEntryId);
                    cmd.Parameters.AddWithValue("@PostedBy", userId);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            bool success = r.GetInt32(0) == 1;
                            string msg = r.IsDBNull(1) ? null : r.GetString(1);
                            return (success, msg);
                        }
                    }
                }
            }
            return (false, "Unknown error posting journal entry");
        }

        public bool DeleteJournalEntry(Guid journalEntryId, Guid userId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                // Only Draft entries can be deleted
                using (var cmd = new SqlCommand("UPDATE acc_JournalEntries SET IsDeleted=1,UpdatedAt=GETUTCDATE(),UpdatedBy=@UserId WHERE Id=@Id AND Status='Draft'", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", journalEntryId);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public Guid CreateReversalEntry(Guid tenantId, Guid userId, Guid originalId, DateTime reverseDate, string description, Guid fiscalPeriodId, string entryNumber)
        {
            var id = Guid.NewGuid();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    // Get original entry
                    JournalEntryDetailDto original = null;
                    const string oSql = @"SELECT je.CurrencyCode, je.ExchangeRate, je.CompanyId, je.BranchId, je.EntryType
                                          FROM acc_JournalEntries je WHERE je.Id=@Id";
                    string currCode = "USD"; decimal exRate = 1; Guid companyId = Guid.Empty; Guid? branchId = null;
                    using (var ocmd = new SqlCommand(oSql, conn, tran))
                    {
                        ocmd.Parameters.AddWithValue("@Id", originalId);
                        using (var or = ocmd.ExecuteReader())
                        {
                            if (or.Read())
                            {
                                currCode  = or.GetString(0);
                                exRate    = or.GetDecimal(1);
                                companyId = or.GetGuid(2);
                                branchId  = or.IsDBNull(3) ? (Guid?)null : or.GetGuid(3);
                            }
                        }
                    }

                    const string sql = @"
                        INSERT INTO acc_JournalEntries(Id,TenantId,CompanyId,BranchId,FiscalPeriodId,EntryNumber,EntryDate,
                                Description,EntryType,CurrencyCode,ExchangeRate,Status,ReversedEntryId,
                                CreatedAt,UpdatedAt,IsDeleted,CreatedBy)
                        VALUES(@Id,@TenantId,@CompanyId,@BranchId,@PeriodId,@EntryNumber,@Date,
                               @Desc,'Reversing',@Currency,@Rate,'Draft',@OriginalId,
                               GETUTCDATE(),GETUTCDATE(),0,@UserId)";
                    using (var cmd = new SqlCommand(sql, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@TenantId", tenantId);
                        cmd.Parameters.AddWithValue("@CompanyId", companyId);
                        cmd.Parameters.AddWithValue("@BranchId", (object)branchId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PeriodId", fiscalPeriodId);
                        cmd.Parameters.AddWithValue("@EntryNumber", entryNumber);
                        cmd.Parameters.AddWithValue("@Date", reverseDate.Date);
                        cmd.Parameters.AddWithValue("@Desc", description);
                        cmd.Parameters.AddWithValue("@Currency", currCode);
                        cmd.Parameters.AddWithValue("@Rate", exRate);
                        cmd.Parameters.AddWithValue("@OriginalId", originalId);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }

                    // Copy lines with Debit/Credit swapped
                    const string lSql = @"
                        INSERT INTO acc_JournalEntryLines(Id,JournalEntryId,TenantId,CompanyId,AccountId,LineNumber,
                                Description,DebitAmount,CreditAmount,CurrencyCode,ExchangeRate,BaseDebit,BaseCredit,Reference,CreatedAt)
                        SELECT NEWID(),@NewId,TenantId,CompanyId,AccountId,LineNumber,
                               Description,CreditAmount,DebitAmount,CurrencyCode,ExchangeRate,BaseCredit,BaseDebit,Reference,GETUTCDATE()
                        FROM acc_JournalEntryLines WHERE JournalEntryId=@OriginalId";
                    using (var lcmd = new SqlCommand(lSql, conn, tran))
                    {
                        lcmd.Parameters.AddWithValue("@NewId", id);
                        lcmd.Parameters.AddWithValue("@OriginalId", originalId);
                        lcmd.ExecuteNonQuery();
                    }

                    tran.Commit();
                }
            }
            return id;
        }

        // ═══════════════════════════════════════════════════════════════════
        // FINANCIAL REPORTS
        // ═══════════════════════════════════════════════════════════════════

        public List<TrialBalanceLineDto> GetTrialBalance(Guid companyId, Guid fiscalYearId, int startPeriod, int endPeriod)
        {
            var list = new List<TrialBalanceLineDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand("EXEC sp_GetTrialBalance @CompanyId, @FiscalYearId, @StartPeriod, @EndPeriod", conn))
                {
                    cmd.Parameters.AddWithValue("@CompanyId", companyId);
                    cmd.Parameters.AddWithValue("@FiscalYearId", fiscalYearId);
                    cmd.Parameters.AddWithValue("@StartPeriod", startPeriod);
                    cmd.Parameters.AddWithValue("@EndPeriod", endPeriod);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapTrialBalanceLine(r));
                }
            }
            return list;
        }

        // ═══════════════════════════════════════════════════════════════════
        // BANK ACCOUNTS
        // ═══════════════════════════════════════════════════════════════════

        public List<BankAccountDto> GetBankAccounts(Guid tenantId, Guid companyId, bool activeOnly = false)
        {
            var list = new List<BankAccountDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var where = "ba.TenantId=@TenantId AND ba.CompanyId=@CompanyId AND ba.IsDeleted=0";
                if (activeOnly) where += " AND ba.IsActive=1";
                var sql = $@"SELECT ba.Id, ba.CompanyId, c.Name AS CompanyName,
                                    ba.BranchId, b.Name AS BranchName,
                                    ba.GlAccountId, a.Code AS GlCode, a.Name AS GlName,
                                    ba.BankName, ba.AccountName, ba.AccountNumber, ba.IBAN, ba.SwiftCode,
                                    ba.CurrencyCode, ba.OpeningBalance, ba.OpeningDate, ba.CurrentBalance,
                                    ba.IsActive, ba.Notes, ba.CreatedAt
                             FROM acc_BankAccounts ba
                             JOIN saas_Companies c ON c.Id=ba.CompanyId
                             LEFT JOIN saas_Branches b ON b.Id=ba.BranchId
                             LEFT JOIN acc_Accounts a ON a.Id=ba.GlAccountId
                             WHERE {where}
                             ORDER BY ba.BankName, ba.AccountName";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    cmd.Parameters.AddWithValue("@CompanyId", companyId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapBankAccount(r));
                }
            }
            return list;
        }

        public BankAccountDto GetBankAccountById(Guid id)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"SELECT ba.Id, ba.CompanyId, c.Name, ba.BranchId, b.Name,
                                            ba.GlAccountId, a.Code, a.Name,
                                            ba.BankName, ba.AccountName, ba.AccountNumber, ba.IBAN, ba.SwiftCode,
                                            ba.CurrencyCode, ba.OpeningBalance, ba.OpeningDate, ba.CurrentBalance,
                                            ba.IsActive, ba.Notes, ba.CreatedAt
                                     FROM acc_BankAccounts ba
                                     JOIN saas_Companies c ON c.Id=ba.CompanyId
                                     LEFT JOIN saas_Branches b ON b.Id=ba.BranchId
                                     LEFT JOIN acc_Accounts a ON a.Id=ba.GlAccountId
                                     WHERE ba.Id=@Id AND ba.IsDeleted=0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var r = cmd.ExecuteReader())
                        return r.Read() ? MapBankAccount(r) : null;
                }
            }
        }

        public Guid CreateBankAccount(Guid tenantId, Guid userId, CreateBankAccountDto dto)
        {
            var id = Guid.NewGuid();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"INSERT INTO acc_BankAccounts(Id,TenantId,CompanyId,BranchId,GlAccountId,BankName,AccountName,
                                      AccountNumber,IBAN,SwiftCode,CurrencyCode,OpeningBalance,OpeningDate,CurrentBalance,IsActive,Notes,
                                      CreatedAt,UpdatedAt,IsDeleted,CreatedBy)
                                     VALUES(@Id,@TenantId,@CompanyId,@BranchId,@GlAccountId,@BankName,@AccountName,
                                            @AccountNumber,@IBAN,@SwiftCode,@CurrencyCode,@OpeningBalance,@OpeningDate,@OpeningBalance,1,@Notes,
                                            GETUTCDATE(),GETUTCDATE(),0,@UserId)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    cmd.Parameters.AddWithValue("@CompanyId", dto.CompanyId);
                    cmd.Parameters.AddWithValue("@BranchId", (object)dto.BranchId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@GlAccountId", (object)dto.GlAccountId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BankName", dto.BankName);
                    cmd.Parameters.AddWithValue("@AccountName", dto.AccountName);
                    cmd.Parameters.AddWithValue("@AccountNumber", dto.AccountNumber);
                    cmd.Parameters.AddWithValue("@IBAN", (object)dto.IBAN ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SwiftCode", (object)dto.SwiftCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CurrencyCode", dto.CurrencyCode ?? "USD");
                    cmd.Parameters.AddWithValue("@OpeningBalance", dto.OpeningBalance);
                    cmd.Parameters.AddWithValue("@OpeningDate", (object)(dto.OpeningDate?.Date) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Notes", (object)dto.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
            return id;
        }

        public bool UpdateBankAccount(Guid userId, UpdateBankAccountDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"UPDATE acc_BankAccounts
                                     SET BranchId=@BranchId, GlAccountId=@GlAccountId, BankName=@BankName, AccountName=@AccountName,
                                         AccountNumber=@AccountNumber, IBAN=@IBAN, SwiftCode=@SwiftCode, CurrencyCode=@CurrencyCode,
                                         IsActive=@IsActive, Notes=@Notes, UpdatedAt=GETUTCDATE()
                                     WHERE Id=@Id AND CompanyId=@CompanyId AND IsDeleted=0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", dto.Id);
                    cmd.Parameters.AddWithValue("@CompanyId", dto.CompanyId);
                    cmd.Parameters.AddWithValue("@BranchId", (object)dto.BranchId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@GlAccountId", (object)dto.GlAccountId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BankName", dto.BankName);
                    cmd.Parameters.AddWithValue("@AccountName", dto.AccountName);
                    cmd.Parameters.AddWithValue("@AccountNumber", dto.AccountNumber);
                    cmd.Parameters.AddWithValue("@IBAN", (object)dto.IBAN ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SwiftCode", (object)dto.SwiftCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CurrencyCode", dto.CurrencyCode ?? "USD");
                    cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);
                    cmd.Parameters.AddWithValue("@Notes", (object)dto.Notes ?? DBNull.Value);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool DeleteBankAccount(Guid id, Guid userId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand("UPDATE acc_BankAccounts SET IsDeleted=1,DeletedAt=GETUTCDATE() WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // BANK RECONCILIATION
        // ═══════════════════════════════════════════════════════════════════

        public List<BankReconciliationDto> GetReconciliations(Guid bankAccountId)
        {
            var list = new List<BankReconciliationDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"SELECT br.Id, br.BankAccountId, ba.AccountName, ba.AccountNumber,
                                            br.StatementDate, br.StatementEndBalance, br.OpeningBalance,
                                            br.ClearedDeposits, br.ClearedWithdrawals, br.ClearedBalance,
                                            br.Difference, br.Status, br.Notes, br.CompletedAt,
                                            u.FullName AS CompletedByName, br.CreatedAt
                                     FROM acc_BankReconciliations br
                                     JOIN acc_BankAccounts ba ON ba.Id=br.BankAccountId
                                     LEFT JOIN sys_Users u ON u.Id=br.CompletedBy
                                     WHERE br.BankAccountId=@BankAccountId
                                     ORDER BY br.StatementDate DESC";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@BankAccountId", bankAccountId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapReconciliation(r));
                }
            }
            return list;
        }

        public Guid CreateReconciliation(Guid tenantId, Guid userId, CreateBankReconciliationDto dto)
        {
            var id = Guid.NewGuid();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                // Get current bank balance as opening
                decimal openingBalance = 0;
                using (var bc = new SqlCommand("SELECT CurrentBalance FROM acc_BankAccounts WHERE Id=@Id", conn))
                {
                    bc.Parameters.AddWithValue("@Id", dto.BankAccountId);
                    var result = bc.ExecuteScalar();
                    if (result != null && result != DBNull.Value) openingBalance = (decimal)result;
                }

                const string sql = @"INSERT INTO acc_BankReconciliations(Id,TenantId,BankAccountId,StatementDate,
                                      StatementEndBalance,OpeningBalance,ClearedDeposits,ClearedWithdrawals,
                                      ClearedBalance,Difference,Status,Notes,CreatedAt,UpdatedAt,CreatedBy)
                                     VALUES(@Id,@TenantId,@BankAccountId,@StatementDate,
                                            @StatementEndBalance,@OpeningBalance,0,0,
                                            @OpeningBalance,@OpeningBalance - @StatementEndBalance,'InProgress',@Notes,GETUTCDATE(),GETUTCDATE(),@UserId)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    cmd.Parameters.AddWithValue("@BankAccountId", dto.BankAccountId);
                    cmd.Parameters.AddWithValue("@StatementDate", dto.StatementDate.Date);
                    cmd.Parameters.AddWithValue("@StatementEndBalance", dto.StatementEndBalance);
                    cmd.Parameters.AddWithValue("@OpeningBalance", openingBalance);
                    cmd.Parameters.AddWithValue("@Notes", (object)dto.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
            return id;
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════════

        private static string GetDefaultNormalBalance(string accountType)
        {
            switch (accountType?.ToLower())
            {
                case "liability":
                case "equity":
                case "revenue":
                    return "Credit";
                default:
                    return "Debit";  // Asset, Expense
            }
        }

        private static void AddJEParams(SqlCommand cmd, Guid companyId, string search, string status,
            DateTime? startDate, DateTime? endDate)
        {
            cmd.Parameters.AddWithValue("@CompanyId", companyId);
            if (!string.IsNullOrWhiteSpace(search))
                cmd.Parameters.AddWithValue("@Search", $"%{search}%");
            if (!string.IsNullOrWhiteSpace(status))
                cmd.Parameters.AddWithValue("@Status", status);
            if (startDate.HasValue)
                cmd.Parameters.AddWithValue("@StartDate", startDate.Value.Date);
            if (endDate.HasValue)
                cmd.Parameters.AddWithValue("@EndDate", endDate.Value.Date);
        }

        // ─── Mappers ─────────────────────────────────────────────────────

        private static CurrencyDto MapCurrency(SqlDataReader r) => new CurrencyDto
        {
            Code = r.GetString(0), Name = r.GetString(1), Symbol = r.GetString(2),
            DecimalPlaces = r.GetInt32(3), IsBaseCurrency = r.GetBoolean(4), IsActive = r.GetBoolean(5)
        };

        private static ExchangeRateDto MapExchangeRate(SqlDataReader r) => new ExchangeRateDto
        {
            Id = r.GetGuid(0), FromCurrency = r.GetString(1), ToCurrency = r.GetString(2),
            Rate = r.GetDecimal(3), EffectiveDate = r.GetDateTime(4),
            Source = r.IsDBNull(5) ? null : r.GetString(5), CreatedAt = r.GetDateTime(6)
        };

        private static FiscalYearDto MapFiscalYear(SqlDataReader r) => new FiscalYearDto
        {
            Id = r.GetGuid(0), CompanyId = r.GetGuid(1), CompanyName = r.GetString(2),
            Year = r.GetInt32(3), StartDate = r.GetDateTime(4), EndDate = r.GetDateTime(5),
            Status = r.GetString(6),
            ClosedAt = r.IsDBNull(7) ? (DateTime?)null : r.GetDateTime(7),
            ClosedByName = r.IsDBNull(8) ? null : r.GetString(8),
            CreatedAt = r.GetDateTime(9)
        };

        private static FiscalPeriodDto MapFiscalPeriod(SqlDataReader r) => new FiscalPeriodDto
        {
            Id = r.GetGuid(0), FiscalYearId = r.GetGuid(1), CompanyId = r.GetGuid(2),
            PeriodNumber = r.GetInt32(3), Name = r.GetString(4),
            StartDate = r.GetDateTime(5), EndDate = r.GetDateTime(6), Status = r.GetString(7),
            ClosedAt = r.IsDBNull(8) ? (DateTime?)null : r.GetDateTime(8)
        };

        private static AccountListItemDto MapAccountListItem(SqlDataReader r) => new AccountListItemDto
        {
            Id = r.GetGuid(0), ParentId = r.IsDBNull(1) ? (Guid?)null : r.GetGuid(1),
            Code = r.GetString(2), Name = r.GetString(3), AccountType = r.GetString(4),
            AccountSubType = r.IsDBNull(5) ? null : r.GetString(5),
            NormalBalance = r.GetString(6),
            CurrencyCode = r.IsDBNull(7) ? null : r.GetString(7),
            IsPosting = r.GetBoolean(8), IsActive = r.GetBoolean(9),
            Level = r.GetInt32(10), SortOrder = r.GetInt32(11)
        };

        private static AccountDto MapAccountDetail(SqlDataReader r) => new AccountDto
        {
            Id = r.GetGuid(0), ParentId = r.IsDBNull(1) ? (Guid?)null : r.GetGuid(1),
            ParentName = r.IsDBNull(2) ? null : r.GetString(2),
            Code = r.GetString(3), Name = r.GetString(4), AccountType = r.GetString(5),
            AccountSubType = r.IsDBNull(6) ? null : r.GetString(6),
            NormalBalance = r.GetString(7),
            CurrencyCode = r.IsDBNull(8) ? null : r.GetString(8),
            IsPosting = r.GetBoolean(9), IsActive = r.GetBoolean(10),
            Description = r.IsDBNull(11) ? null : r.GetString(11),
            Level = r.GetInt32(12), SortOrder = r.GetInt32(13), CreatedAt = r.GetDateTime(14)
        };

        private static JournalEntryListItemDto MapJEListItem(SqlDataReader r) => new JournalEntryListItemDto
        {
            Id = r.GetGuid(0), EntryNumber = r.GetString(1), EntryDate = r.GetDateTime(2),
            Description = r.GetString(3), Reference = r.IsDBNull(4) ? null : r.GetString(4),
            EntryType = r.GetString(5), CurrencyCode = r.GetString(6), ExchangeRate = r.GetDecimal(7),
            TotalDebit = r.GetDecimal(8), TotalCredit = r.GetDecimal(9), Status = r.GetString(10),
            CreatedByName = r.IsDBNull(11) ? null : r.GetString(11),
            PostedByName = r.IsDBNull(12) ? null : r.GetString(12),
            PostedAt = r.IsDBNull(13) ? (DateTime?)null : r.GetDateTime(13),
            CreatedAt = r.GetDateTime(14), LineCount = r.GetInt32(15)
        };

        private static JournalEntryDetailDto MapJEDetail(SqlDataReader r) => new JournalEntryDetailDto
        {
            Id = r.GetGuid(0), EntryNumber = r.GetString(1), EntryDate = r.GetDateTime(2),
            Description = r.GetString(3), Reference = r.IsDBNull(4) ? null : r.GetString(4),
            EntryType = r.GetString(5), CurrencyCode = r.GetString(6), ExchangeRate = r.GetDecimal(7),
            TotalDebit = r.GetDecimal(8), TotalCredit = r.GetDecimal(9),
            BaseCurrencyDebit = r.GetDecimal(10), BaseCurrencyCredit = r.GetDecimal(11),
            Status = r.GetString(12),
            CreatedByName = r.IsDBNull(13) ? null : r.GetString(13),
            PostedByName = r.IsDBNull(14) ? null : r.GetString(14),
            PostedAt = r.IsDBNull(15) ? (DateTime?)null : r.GetDateTime(15),
            CreatedAt = r.GetDateTime(16), Notes = r.IsDBNull(17) ? null : r.GetString(17),
            ReversedEntryId = r.IsDBNull(18) ? (Guid?)null : r.GetGuid(18),
            ReversedEntryNumber = r.IsDBNull(19) ? null : r.GetString(19),
            CompanyId = r.GetGuid(20), CompanyName = r.IsDBNull(21) ? null : r.GetString(21),
            BranchId = r.IsDBNull(22) ? (Guid?)null : r.GetGuid(22),
            BranchName = r.IsDBNull(23) ? null : r.GetString(23),
            FiscalPeriodId = r.GetGuid(24), FiscalPeriodName = r.IsDBNull(25) ? null : r.GetString(25),
            LineCount = r.GetInt32(26)
        };

        private static JournalEntryLineDto MapJELine(SqlDataReader r) => new JournalEntryLineDto
        {
            Id = r.GetGuid(0), LineNumber = r.GetInt32(1), AccountId = r.GetGuid(2),
            AccountCode = r.GetString(3), AccountName = r.GetString(4), AccountType = r.GetString(5),
            Description = r.IsDBNull(6) ? null : r.GetString(6),
            DebitAmount = r.GetDecimal(7), CreditAmount = r.GetDecimal(8),
            CurrencyCode = r.GetString(9), ExchangeRate = r.GetDecimal(10),
            BaseDebit = r.GetDecimal(11), BaseCredit = r.GetDecimal(12),
            Reference = r.IsDBNull(13) ? null : r.GetString(13)
        };

        private static TrialBalanceLineDto MapTrialBalanceLine(SqlDataReader r) => new TrialBalanceLineDto
        {
            AccountId = r.GetGuid(0), Code = r.GetString(1), Name = r.GetString(2),
            AccountType = r.GetString(3), AccountSubType = r.IsDBNull(4) ? null : r.GetString(4),
            NormalBalance = r.GetString(5), Level = r.GetInt32(6),
            ParentId = r.IsDBNull(7) ? (Guid?)null : r.GetGuid(7),
            OpeningDebit = r.GetDecimal(8), OpeningCredit = r.GetDecimal(9),
            PeriodDebit = r.GetDecimal(10), PeriodCredit = r.GetDecimal(11),
            ClosingDebit = r.GetDecimal(12), ClosingCredit = r.GetDecimal(13)
        };

        private static BankAccountDto MapBankAccount(SqlDataReader r) => new BankAccountDto
        {
            Id = r.GetGuid(0), CompanyId = r.GetGuid(1), CompanyName = r.GetString(2),
            BranchId = r.IsDBNull(3) ? (Guid?)null : r.GetGuid(3),
            BranchName = r.IsDBNull(4) ? null : r.GetString(4),
            GlAccountId = r.IsDBNull(5) ? (Guid?)null : r.GetGuid(5),
            GlAccountCode = r.IsDBNull(6) ? null : r.GetString(6),
            GlAccountName = r.IsDBNull(7) ? null : r.GetString(7),
            BankName = r.GetString(8), AccountName = r.GetString(9), AccountNumber = r.GetString(10),
            IBAN = r.IsDBNull(11) ? null : r.GetString(11), SwiftCode = r.IsDBNull(12) ? null : r.GetString(12),
            CurrencyCode = r.GetString(13), OpeningBalance = r.GetDecimal(14),
            OpeningDate = r.IsDBNull(15) ? (DateTime?)null : r.GetDateTime(15),
            CurrentBalance = r.GetDecimal(16), IsActive = r.GetBoolean(17),
            Notes = r.IsDBNull(18) ? null : r.GetString(18), CreatedAt = r.GetDateTime(19)
        };

        private static BankReconciliationDto MapReconciliation(SqlDataReader r) => new BankReconciliationDto
        {
            Id = r.GetGuid(0), BankAccountId = r.GetGuid(1),
            BankAccountName = r.GetString(2), AccountNumber = r.GetString(3),
            StatementDate = r.GetDateTime(4), StatementEndBalance = r.GetDecimal(5),
            OpeningBalance = r.GetDecimal(6), ClearedDeposits = r.GetDecimal(7),
            ClearedWithdrawals = r.GetDecimal(8), ClearedBalance = r.GetDecimal(9),
            Difference = r.GetDecimal(10), Status = r.GetString(11),
            Notes = r.IsDBNull(12) ? null : r.GetString(12),
            CompletedAt = r.IsDBNull(13) ? (DateTime?)null : r.GetDateTime(13),
            CompletedByName = r.IsDBNull(14) ? null : r.GetString(14),
            CreatedAt = r.GetDateTime(15)
        };
    }
}
