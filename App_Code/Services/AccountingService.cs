using System;
using System.Collections.Generic;
using System.Linq;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Caching;
using EidUbahle.Repositories;

namespace EidUbahle.Services
{
    /// <summary>
    /// Core Accounting Service – Phase 3.
    /// Handles: Chart of Accounts, Journal Entries (with double-entry validation),
    /// Fiscal Periods, Financial Reports, Bank Accounts, Currencies & Exchange Rates.
    /// </summary>
    public class AccountingService
    {
        private readonly AccountingRepository _repo;
        private readonly IAppCache _cache;

        public AccountingService(string connectionString, IAppCache cache)
        {
            _repo = new AccountingRepository(connectionString);
            _cache = cache;
        }

        // ═══════════════════════════════════════════════════════════════════
        // CURRENCIES & EXCHANGE RATES
        // ═══════════════════════════════════════════════════════════════════

        public ApiResponseDto<List<CurrencyDto>> GetCurrencies(bool activeOnly = true)
        {
            var data = _cache.GetOrAdd("currencies:all", () => _repo.GetCurrencies(activeOnly), TimeSpan.FromHours(1));
            return ApiResponseDto<List<CurrencyDto>>.Ok(data);
        }

        public ApiResponseDto<List<ExchangeRateDto>> GetExchangeRates(Guid tenantId, string fromCurrency = null)
        {
            var data = _repo.GetExchangeRates(tenantId, fromCurrency);
            return ApiResponseDto<List<ExchangeRateDto>>.Ok(data);
        }

        public ApiResponseDto<Guid> CreateExchangeRate(Guid tenantId, Guid userId, CreateExchangeRateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FromCurrency) || string.IsNullOrWhiteSpace(dto.ToCurrency))
                return ApiResponseDto<Guid>.Fail("From and To currencies are required", "ERR_VALIDATION");
            if (dto.FromCurrency == dto.ToCurrency)
                return ApiResponseDto<Guid>.Fail("From and To currencies must be different", "ERR_VALIDATION");
            if (dto.Rate <= 0)
                return ApiResponseDto<Guid>.Fail("Exchange rate must be greater than zero", "ERR_VALIDATION");

            var id = _repo.CreateExchangeRate(tenantId, userId, dto);
            return ApiResponseDto<Guid>.Ok(id, "Exchange rate saved");
        }

        // ═══════════════════════════════════════════════════════════════════
        // FISCAL YEARS & PERIODS
        // ═══════════════════════════════════════════════════════════════════

        public ApiResponseDto<List<FiscalYearDto>> GetFiscalYears(Guid tenantId, Guid companyId)
        {
            var data = _repo.GetFiscalYears(tenantId, companyId);
            return ApiResponseDto<List<FiscalYearDto>>.Ok(data);
        }

        public ApiResponseDto<FiscalYearDto> GetFiscalYear(Guid fiscalYearId)
        {
            var data = _repo.GetFiscalYear(fiscalYearId);
            if (data == null) return ApiResponseDto<FiscalYearDto>.Fail("Fiscal year not found", "ERR_NOT_FOUND");
            return ApiResponseDto<FiscalYearDto>.Ok(data);
        }

        public ApiResponseDto<Guid> CreateFiscalYear(Guid tenantId, Guid userId, CreateFiscalYearDto dto)
        {
            if (dto.CompanyId == Guid.Empty)
                return ApiResponseDto<Guid>.Fail("Company is required", "ERR_VALIDATION");
            if (dto.Year < 2000 || dto.Year > 2100)
                return ApiResponseDto<Guid>.Fail("Year must be between 2000 and 2100", "ERR_VALIDATION");
            if (dto.StartDate >= dto.EndDate)
                return ApiResponseDto<Guid>.Fail("Start date must be before end date", "ERR_VALIDATION");

            if (_repo.FiscalYearExists(dto.CompanyId, dto.Year))
                return ApiResponseDto<Guid>.Fail($"Fiscal year {dto.Year} already exists for this company", "ERR_DUPLICATE");

            var id = _repo.CreateFiscalYear(tenantId, userId, dto);
            return ApiResponseDto<Guid>.Ok(id, $"Fiscal year {dto.Year} created with 12 periods");
        }

        public ApiResponseDto<bool> CloseFiscalPeriod(Guid userId, CloseFiscalPeriodDto dto)
        {
            _repo.CloseFiscalPeriod(dto.FiscalPeriodId, userId);
            return ApiResponseDto<bool>.Ok(true, "Fiscal period closed");
        }

        public ApiResponseDto<bool> CloseFiscalYear(Guid userId, CloseFiscalYearDto dto)
        {
            _repo.CloseFiscalYear(dto.FiscalYearId, userId);
            return ApiResponseDto<bool>.Ok(true, "Fiscal year closed");
        }

        // ═══════════════════════════════════════════════════════════════════
        // CHART OF ACCOUNTS
        // ═══════════════════════════════════════════════════════════════════

        public ApiResponseDto<List<AccountListItemDto>> GetAccounts(Guid companyId, bool activeOnly = false, string accountType = null)
        {
            var data = _repo.GetAccounts(companyId, activeOnly, accountType);
            return ApiResponseDto<List<AccountListItemDto>>.Ok(data);
        }

        public ApiResponseDto<AccountDto> GetAccount(Guid accountId)
        {
            var data = _repo.GetAccountById(accountId);
            if (data == null) return ApiResponseDto<AccountDto>.Fail("Account not found", "ERR_NOT_FOUND");
            return ApiResponseDto<AccountDto>.Ok(data);
        }

        public ApiResponseDto<Guid> CreateAccount(Guid tenantId, Guid userId, CreateAccountDto dto)
        {
            if (dto.CompanyId == Guid.Empty)
                return ApiResponseDto<Guid>.Fail("Company is required", "ERR_VALIDATION");
            if (string.IsNullOrWhiteSpace(dto.Code))
                return ApiResponseDto<Guid>.Fail("Account code is required", "ERR_VALIDATION");
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponseDto<Guid>.Fail("Account name is required", "ERR_VALIDATION");
            if (string.IsNullOrWhiteSpace(dto.AccountType))
                return ApiResponseDto<Guid>.Fail("Account type is required", "ERR_VALIDATION");

            var validTypes = new[] { "Asset", "Liability", "Equity", "Revenue", "Expense" };
            if (!Array.Exists(validTypes, t => t.Equals(dto.AccountType, StringComparison.OrdinalIgnoreCase)))
                return ApiResponseDto<Guid>.Fail($"Invalid account type. Must be one of: {string.Join(", ", validTypes)}", "ERR_VALIDATION");

            if (_repo.AccountCodeExists(dto.CompanyId, dto.Code))
                return ApiResponseDto<Guid>.Fail($"Account code '{dto.Code}' already exists", "ERR_DUPLICATE");

            var id = _repo.CreateAccount(tenantId, userId, dto);
            _cache.Remove($"accounts:{dto.CompanyId}");
            return ApiResponseDto<Guid>.Ok(id, "Account created");
        }

        public ApiResponseDto<bool> UpdateAccount(Guid tenantId, Guid userId, UpdateAccountDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponseDto<bool>.Fail("Account name is required", "ERR_VALIDATION");

            if (_repo.AccountCodeExists(dto.CompanyId, dto.Code, dto.Id))
                return ApiResponseDto<bool>.Fail($"Account code '{dto.Code}' already exists", "ERR_DUPLICATE");

            var success = _repo.UpdateAccount(tenantId, userId, dto);
            if (!success) return ApiResponseDto<bool>.Fail("Account not found or cannot be updated", "ERR_NOT_FOUND");
            _cache.Remove($"accounts:{dto.CompanyId}");
            return ApiResponseDto<bool>.Ok(true, "Account updated");
        }

        public ApiResponseDto<bool> DeleteAccount(Guid accountId, Guid userId)
        {
            if (_repo.AccountHasTransactions(accountId))
                return ApiResponseDto<bool>.Fail("Cannot delete account with posted transactions", "ERR_HAS_TRANSACTIONS");

            var success = _repo.DeleteAccount(accountId, userId);
            if (!success) return ApiResponseDto<bool>.Fail("Account not found or has posted transactions", "ERR_NOT_FOUND");
            return ApiResponseDto<bool>.Ok(true, "Account deleted");
        }

        // ═══════════════════════════════════════════════════════════════════
        // JOURNAL ENTRIES
        // ═══════════════════════════════════════════════════════════════════

        public ApiResponseDto<PagedResultDto<JournalEntryListItemDto>> GetJournalEntries(
            Guid tenantId, Guid companyId, string search = null, string status = null,
            DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20)
        {
            var data = _repo.GetJournalEntries(tenantId, companyId, search, status, startDate, endDate,
                Math.Max(1, page), Math.Min(pageSize, 100));
            return ApiResponseDto<PagedResultDto<JournalEntryListItemDto>>.Ok(data);
        }

        public ApiResponseDto<JournalEntryDetailDto> GetJournalEntry(Guid journalEntryId)
        {
            var data = _repo.GetJournalEntry(journalEntryId);
            if (data == null) return ApiResponseDto<JournalEntryDetailDto>.Fail("Journal entry not found", "ERR_NOT_FOUND");
            return ApiResponseDto<JournalEntryDetailDto>.Ok(data);
        }

        public ApiResponseDto<Guid> CreateJournalEntry(Guid tenantId, Guid userId, CreateJournalEntryDto dto)
        {
            // Validate header
            if (dto.CompanyId == Guid.Empty)
                return ApiResponseDto<Guid>.Fail("Company is required", "ERR_VALIDATION");
            if (string.IsNullOrWhiteSpace(dto.Description))
                return ApiResponseDto<Guid>.Fail("Description is required", "ERR_VALIDATION");
            if (dto.EntryDate == default)
                return ApiResponseDto<Guid>.Fail("Entry date is required", "ERR_VALIDATION");
            if (dto.Lines == null || dto.Lines.Count < 2)
                return ApiResponseDto<Guid>.Fail("Journal entry must have at least 2 lines", "ERR_VALIDATION");

            // Validate double-entry balance
            decimal totalDebit  = dto.Lines.Sum(l => l.DebitAmount);
            decimal totalCredit = dto.Lines.Sum(l => l.CreditAmount);
            if (Math.Abs(totalDebit - totalCredit) > 0.005m)
                return ApiResponseDto<Guid>.Fail(
                    $"Journal entry is not balanced. Debit: {totalDebit:N2}, Credit: {totalCredit:N2}",
                    "ERR_NOT_BALANCED");

            // Validate lines
            foreach (var line in dto.Lines)
            {
                if (line.AccountId == Guid.Empty)
                    return ApiResponseDto<Guid>.Fail("Each line must have an account", "ERR_VALIDATION");
                if (line.DebitAmount < 0 || line.CreditAmount < 0)
                    return ApiResponseDto<Guid>.Fail("Line amounts cannot be negative", "ERR_VALIDATION");
                if (line.DebitAmount == 0 && line.CreditAmount == 0)
                    return ApiResponseDto<Guid>.Fail("Each line must have a debit or credit amount", "ERR_VALIDATION");
                if (line.DebitAmount > 0 && line.CreditAmount > 0)
                    return ApiResponseDto<Guid>.Fail("A line cannot have both debit and credit amounts", "ERR_VALIDATION");
            }

            // Find fiscal period
            var period = _repo.GetOpenPeriodForDate(dto.CompanyId, dto.EntryDate);
            if (period == null)
                return ApiResponseDto<Guid>.Fail(
                    $"No open fiscal period found for date {dto.EntryDate:yyyy-MM-dd}. Please create or open a fiscal period.",
                    "ERR_NO_FISCAL_PERIOD");

            // Generate entry number
            var entryNumber = _repo.GenerateEntryNumber(dto.CompanyId, dto.EntryDate);

            var id = _repo.CreateJournalEntry(tenantId, userId, dto, period.Id, entryNumber);

            // Post immediately if requested
            if (dto.PostImmediately)
            {
                var (success, error) = _repo.PostJournalEntry(id, userId);
                if (!success)
                    return ApiResponseDto<Guid>.Fail($"Entry saved but posting failed: {error}", "ERR_POST_FAILED");
            }

            return ApiResponseDto<Guid>.Ok(id, dto.PostImmediately ? "Journal entry created and posted" : "Journal entry saved as draft");
        }

        public ApiResponseDto<bool> PostJournalEntry(Guid tenantId, Guid userId, PostJournalEntryDto dto)
        {
            var (success, error) = _repo.PostJournalEntry(dto.JournalEntryId, userId);
            if (!success) return ApiResponseDto<bool>.Fail(error ?? "Failed to post journal entry", "ERR_POST_FAILED");
            return ApiResponseDto<bool>.Ok(true, "Journal entry posted successfully");
        }

        public ApiResponseDto<bool> DeleteJournalEntry(Guid tenantId, Guid userId, Guid journalEntryId)
        {
            var success = _repo.DeleteJournalEntry(journalEntryId, userId);
            if (!success) return ApiResponseDto<bool>.Fail("Journal entry not found or is already posted", "ERR_NOT_FOUND");
            return ApiResponseDto<bool>.Ok(true, "Draft journal entry deleted");
        }

        public ApiResponseDto<Guid> ReverseJournalEntry(Guid tenantId, Guid userId, ReverseJournalEntryDto dto)
        {
            var original = _repo.GetJournalEntry(dto.JournalEntryId);
            if (original == null)
                return ApiResponseDto<Guid>.Fail("Journal entry not found", "ERR_NOT_FOUND");
            if (original.Status != "Posted")
                return ApiResponseDto<Guid>.Fail("Only posted journal entries can be reversed", "ERR_INVALID_STATUS");

            // Find fiscal period for reversal date
            var period = _repo.GetOpenPeriodForDate(original.CompanyId, dto.ReverseDate);
            if (period == null)
                return ApiResponseDto<Guid>.Fail(
                    $"No open fiscal period for reversal date {dto.ReverseDate:yyyy-MM-dd}",
                    "ERR_NO_FISCAL_PERIOD");

            var entryNumber = _repo.GenerateEntryNumber(original.CompanyId, dto.ReverseDate);
            var description = string.IsNullOrWhiteSpace(dto.Description)
                ? $"Reversal of {original.EntryNumber}"
                : dto.Description;

            var id = _repo.CreateReversalEntry(tenantId, userId, dto.JournalEntryId,
                dto.ReverseDate, description, period.Id, entryNumber);

            // Auto-post the reversal
            _repo.PostJournalEntry(id, userId);

            return ApiResponseDto<Guid>.Ok(id, "Reversal entry created and posted");
        }

        // ═══════════════════════════════════════════════════════════════════
        // FINANCIAL REPORTS
        // ═══════════════════════════════════════════════════════════════════

        public ApiResponseDto<TrialBalanceDto> GetTrialBalance(Guid tenantId, ReportFilterDto filter)
        {
            if (filter.CompanyId == Guid.Empty)
                return ApiResponseDto<TrialBalanceDto>.Fail("Company is required", "ERR_VALIDATION");
            if (!filter.FiscalYearId.HasValue || filter.FiscalYearId == Guid.Empty)
                return ApiResponseDto<TrialBalanceDto>.Fail("Fiscal year is required", "ERR_VALIDATION");

            int start = filter.StartPeriod ?? 1;
            int end   = filter.EndPeriod   ?? 12;

            var lines = _repo.GetTrialBalance(filter.CompanyId, filter.FiscalYearId.Value, start, end);
            var fy    = _repo.GetFiscalYear(filter.FiscalYearId.Value);

            // Calculate net balance per account
            foreach (var line in lines)
            {
                line.Balance = line.NormalBalance == "Debit"
                    ? line.ClosingDebit - line.ClosingCredit
                    : line.ClosingCredit - line.ClosingDebit;
            }

            if (!filter.IncludeZeroBalances)
                lines = lines.FindAll(l => l.ClosingDebit != 0 || l.ClosingCredit != 0);

            var tb = new TrialBalanceDto
            {
                CompanyId = filter.CompanyId,
                CompanyName = fy?.CompanyName ?? "",
                FiscalYearName = fy != null ? $"FY {fy.Year}" : "",
                PeriodRange = start == end ? $"Period {start}" : $"Periods {start}–{end}",
                AsOf = DateTime.UtcNow,
                Lines = lines,
                TotalDebit  = lines.Sum(l => l.ClosingDebit),
                TotalCredit = lines.Sum(l => l.ClosingCredit)
            };
            tb.IsBalanced = Math.Abs(tb.TotalDebit - tb.TotalCredit) < 0.01m;

            return ApiResponseDto<TrialBalanceDto>.Ok(tb);
        }

        public ApiResponseDto<BalanceSheetDto> GetBalanceSheet(Guid tenantId, ReportFilterDto filter)
        {
            var tbResult = GetTrialBalance(tenantId, filter);
            if (!tbResult.Success) return ApiResponseDto<BalanceSheetDto>.Fail(tbResult.Message, tbResult.ErrorCode);

            var fy = _repo.GetFiscalYear(filter.FiscalYearId.Value);
            var lines = tbResult.Data.Lines;

            var bs = new BalanceSheetDto
            {
                CompanyId = filter.CompanyId,
                CompanyName = tbResult.Data.CompanyName,
                FiscalYearName = tbResult.Data.FiscalYearName,
                AsOf = DateTime.UtcNow,
                Assets = BuildSection("Assets", lines, "Asset"),
                Liabilities = BuildSection("Liabilities", lines, "Liability"),
                Equity = BuildSection("Equity", lines, "Equity")
            };
            bs.TotalAssets = bs.Assets.Total;
            bs.TotalLiabilities = bs.Liabilities.Total;
            bs.TotalEquity = bs.Equity.Total;
            bs.TotalLiabilitiesAndEquity = bs.TotalLiabilities + bs.TotalEquity;
            bs.IsBalanced = Math.Abs(bs.TotalAssets - bs.TotalLiabilitiesAndEquity) < 0.01m;

            return ApiResponseDto<BalanceSheetDto>.Ok(bs);
        }

        public ApiResponseDto<IncomeStatementDto> GetIncomeStatement(Guid tenantId, ReportFilterDto filter)
        {
            var tbResult = GetTrialBalance(tenantId, filter);
            if (!tbResult.Success) return ApiResponseDto<IncomeStatementDto>.Fail(tbResult.Message, tbResult.ErrorCode);

            var fy = _repo.GetFiscalYear(filter.FiscalYearId.Value);
            var lines = tbResult.Data.Lines;

            var revenueLines = lines.FindAll(l => l.AccountType == "Revenue");
            var expenseLines = lines.FindAll(l => l.AccountType == "Expense");

            decimal totalRevenue  = revenueLines.Sum(l => l.Balance);
            decimal totalExpenses = expenseLines.Sum(l => l.Balance);

            // Split expenses into Cost of Sales vs Operating vs Other
            var cosLines  = expenseLines.FindAll(l => l.AccountSubType == "CostOfSales");
            var opexLines = expenseLines.FindAll(l => l.AccountSubType != "CostOfSales" && l.AccountSubType != "OtherExpense");
            var otherExp  = expenseLines.FindAll(l => l.AccountSubType == "OtherExpense");
            var otherInc  = revenueLines.FindAll(l => l.AccountSubType == "OtherIncome");
            var mainRev   = revenueLines.FindAll(l => l.AccountSubType != "OtherIncome");

            var grossProfit = mainRev.Sum(l => l.Balance) - cosLines.Sum(l => l.Balance);

            var is_ = new IncomeStatementDto
            {
                CompanyId = filter.CompanyId,
                CompanyName = tbResult.Data.CompanyName,
                FiscalYearName = tbResult.Data.FiscalYearName,
                PeriodRange = tbResult.Data.PeriodRange,
                TotalRevenue = totalRevenue,
                TotalExpenses = totalExpenses,
                GrossProfit = grossProfit,
                NetIncome = totalRevenue - totalExpenses,
                Revenue = new IncomeStatementSectionDto { Title = "Revenue", Lines = mainRev, Total = mainRev.Sum(l => l.Balance) },
                CostOfSales = new IncomeStatementSectionDto { Title = "Cost of Sales", Lines = cosLines, Total = cosLines.Sum(l => l.Balance) },
                OperatingExpenses = new IncomeStatementSectionDto { Title = "Operating Expenses", Lines = opexLines, Total = opexLines.Sum(l => l.Balance) },
                OtherIncome = new IncomeStatementSectionDto { Title = "Other Income", Lines = otherInc, Total = otherInc.Sum(l => l.Balance) },
                OtherExpenses = new IncomeStatementSectionDto { Title = "Other Expenses", Lines = otherExp, Total = otherExp.Sum(l => l.Balance) }
            };

            return ApiResponseDto<IncomeStatementDto>.Ok(is_);
        }

        // ═══════════════════════════════════════════════════════════════════
        // BANK ACCOUNTS
        // ═══════════════════════════════════════════════════════════════════

        public ApiResponseDto<List<BankAccountDto>> GetBankAccounts(Guid tenantId, Guid companyId, bool activeOnly = false)
        {
            var data = _repo.GetBankAccounts(tenantId, companyId, activeOnly);
            return ApiResponseDto<List<BankAccountDto>>.Ok(data);
        }

        public ApiResponseDto<BankAccountDto> GetBankAccount(Guid bankAccountId)
        {
            var data = _repo.GetBankAccountById(bankAccountId);
            if (data == null) return ApiResponseDto<BankAccountDto>.Fail("Bank account not found", "ERR_NOT_FOUND");
            return ApiResponseDto<BankAccountDto>.Ok(data);
        }

        public ApiResponseDto<Guid> CreateBankAccount(Guid tenantId, Guid userId, CreateBankAccountDto dto)
        {
            if (dto.CompanyId == Guid.Empty)
                return ApiResponseDto<Guid>.Fail("Company is required", "ERR_VALIDATION");
            if (string.IsNullOrWhiteSpace(dto.BankName))
                return ApiResponseDto<Guid>.Fail("Bank name is required", "ERR_VALIDATION");
            if (string.IsNullOrWhiteSpace(dto.AccountNumber))
                return ApiResponseDto<Guid>.Fail("Account number is required", "ERR_VALIDATION");
            if (string.IsNullOrWhiteSpace(dto.CurrencyCode))
                return ApiResponseDto<Guid>.Fail("Currency is required", "ERR_VALIDATION");

            var id = _repo.CreateBankAccount(tenantId, userId, dto);
            return ApiResponseDto<Guid>.Ok(id, "Bank account created");
        }

        public ApiResponseDto<bool> UpdateBankAccount(Guid userId, UpdateBankAccountDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.BankName))
                return ApiResponseDto<bool>.Fail("Bank name is required", "ERR_VALIDATION");

            var success = _repo.UpdateBankAccount(userId, dto);
            if (!success) return ApiResponseDto<bool>.Fail("Bank account not found", "ERR_NOT_FOUND");
            return ApiResponseDto<bool>.Ok(true, "Bank account updated");
        }

        public ApiResponseDto<bool> DeleteBankAccount(Guid bankAccountId, Guid userId)
        {
            var success = _repo.DeleteBankAccount(bankAccountId, userId);
            if (!success) return ApiResponseDto<bool>.Fail("Bank account not found", "ERR_NOT_FOUND");
            return ApiResponseDto<bool>.Ok(true, "Bank account deleted");
        }

        public ApiResponseDto<List<BankReconciliationDto>> GetReconciliations(Guid bankAccountId)
        {
            var data = _repo.GetReconciliations(bankAccountId);
            return ApiResponseDto<List<BankReconciliationDto>>.Ok(data);
        }

        public ApiResponseDto<Guid> CreateReconciliation(Guid tenantId, Guid userId, CreateBankReconciliationDto dto)
        {
            if (dto.BankAccountId == Guid.Empty)
                return ApiResponseDto<Guid>.Fail("Bank account is required", "ERR_VALIDATION");
            if (dto.StatementDate == default)
                return ApiResponseDto<Guid>.Fail("Statement date is required", "ERR_VALIDATION");

            var id = _repo.CreateReconciliation(tenantId, userId, dto);
            return ApiResponseDto<Guid>.Ok(id, "Reconciliation started");
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════════

        private static BalanceSheetSectionDto BuildSection(string title, List<TrialBalanceLineDto> allLines, string accountType)
        {
            var sectionLines = allLines.FindAll(l => l.AccountType == accountType);
            var groups = sectionLines
                .GroupBy(l => l.AccountSubType ?? accountType)
                .Select(g => new BalanceSheetGroupDto
                {
                    SubType = g.Key,
                    Lines = g.ToList(),
                    Total = g.Sum(l => l.Balance)
                }).ToList();

            return new BalanceSheetSectionDto
            {
                Title = title,
                Groups = groups,
                Total = sectionLines.Sum(l => l.Balance)
            };
        }
    }
}
