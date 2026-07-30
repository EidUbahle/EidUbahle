using System;
using System.Collections.Generic;

namespace EidUbahle.Domain.DTOs
{
    // ─── Currencies ─────────────────────────────────────────────────────────────

    public class CurrencyDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public int DecimalPlaces { get; set; }
        public bool IsBaseCurrency { get; set; }
        public bool IsActive { get; set; }
    }

    public class ExchangeRateDto
    {
        public Guid Id { get; set; }
        public string FromCurrency { get; set; }
        public string ToCurrency { get; set; }
        public decimal Rate { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string Source { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateExchangeRateDto
    {
        public string FromCurrency { get; set; }
        public string ToCurrency { get; set; }
        public decimal Rate { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string Source { get; set; } = "Manual";
    }

    // ─── Fiscal Years / Periods ──────────────────────────────────────────────────

    public class FiscalYearDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; }
        public int Year { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }         // Open | Closed | Locked
        public DateTime? ClosedAt { get; set; }
        public string ClosedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int OpenPeriodCount { get; set; }
        public int ClosedPeriodCount { get; set; }
        public List<FiscalPeriodDto> Periods { get; set; } = new List<FiscalPeriodDto>();
    }

    public class FiscalPeriodDto
    {
        public Guid Id { get; set; }
        public Guid FiscalYearId { get; set; }
        public Guid CompanyId { get; set; }
        public int PeriodNumber { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }         // Open | Closed | Locked
        public DateTime? ClosedAt { get; set; }
    }

    public class CreateFiscalYearDto
    {
        public Guid CompanyId { get; set; }
        public int Year { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class CloseFiscalPeriodDto
    {
        public Guid FiscalPeriodId { get; set; }
        public string Reason { get; set; }
    }

    public class CloseFiscalYearDto
    {
        public Guid FiscalYearId { get; set; }
        public string Reason { get; set; }
    }

    // ─── Chart of Accounts ───────────────────────────────────────────────────────

    public class AccountDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public Guid? ParentId { get; set; }
        public string ParentName { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string AccountType { get; set; }    // Asset | Liability | Equity | Revenue | Expense
        public string AccountSubType { get; set; }
        public string NormalBalance { get; set; }  // Debit | Credit
        public string CurrencyCode { get; set; }
        public bool IsPosting { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }
        public int Level { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<AccountDto> Children { get; set; } = new List<AccountDto>();

        // Balance fields (populated for reports)
        public decimal OpeningDebit { get; set; }
        public decimal OpeningCredit { get; set; }
        public decimal PeriodDebit { get; set; }
        public decimal PeriodCredit { get; set; }
        public decimal ClosingDebit { get; set; }
        public decimal ClosingCredit { get; set; }
        public decimal Balance { get; set; }   // net balance based on NormalBalance
    }

    public class CreateAccountDto
    {
        public Guid CompanyId { get; set; }
        public Guid? ParentId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string AccountType { get; set; }
        public string AccountSubType { get; set; }
        public string CurrencyCode { get; set; }
        public bool IsPosting { get; set; } = true;
        public string Description { get; set; }
        public int SortOrder { get; set; } = 0;
    }

    public class UpdateAccountDto : CreateAccountDto
    {
        public Guid Id { get; set; }
        public bool IsActive { get; set; }
    }

    public class AccountListItemDto
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string AccountType { get; set; }
        public string AccountSubType { get; set; }
        public string NormalBalance { get; set; }
        public string CurrencyCode { get; set; }
        public bool IsPosting { get; set; }
        public bool IsActive { get; set; }
        public int Level { get; set; }
        public int SortOrder { get; set; }
    }

    // ─── Journal Entries ─────────────────────────────────────────────────────────

    public class JournalEntryListItemDto
    {
        public Guid Id { get; set; }
        public string EntryNumber { get; set; }
        public DateTime EntryDate { get; set; }
        public string Description { get; set; }
        public string Reference { get; set; }
        public string EntryType { get; set; }
        public string CurrencyCode { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public string Status { get; set; }
        public string CreatedByName { get; set; }
        public string PostedByName { get; set; }
        public DateTime? PostedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int LineCount { get; set; }
    }

    public class JournalEntryDetailDto : JournalEntryListItemDto
    {
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; }
        public Guid? BranchId { get; set; }
        public string BranchName { get; set; }
        public Guid FiscalPeriodId { get; set; }
        public string FiscalPeriodName { get; set; }
        public decimal BaseCurrencyDebit { get; set; }
        public decimal BaseCurrencyCredit { get; set; }
        public string Notes { get; set; }
        public Guid? ReversedEntryId { get; set; }
        public string ReversedEntryNumber { get; set; }
        public List<JournalEntryLineDto> Lines { get; set; } = new List<JournalEntryLineDto>();
    }

    public class JournalEntryLineDto
    {
        public Guid Id { get; set; }
        public int LineNumber { get; set; }
        public Guid AccountId { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public string AccountType { get; set; }
        public string Description { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public string CurrencyCode { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal BaseDebit { get; set; }
        public decimal BaseCredit { get; set; }
        public string Reference { get; set; }
    }

    public class CreateJournalEntryDto
    {
        public Guid CompanyId { get; set; }
        public Guid? BranchId { get; set; }
        public DateTime EntryDate { get; set; }
        public string Description { get; set; }
        public string Reference { get; set; }
        public string EntryType { get; set; } = "Normal";
        public string CurrencyCode { get; set; } = "USD";
        public decimal ExchangeRate { get; set; } = 1;
        public string Notes { get; set; }
        public List<CreateJournalEntryLineDto> Lines { get; set; } = new List<CreateJournalEntryLineDto>();
        public bool PostImmediately { get; set; } = false;
    }

    public class CreateJournalEntryLineDto
    {
        public int LineNumber { get; set; }
        public Guid AccountId { get; set; }
        public string Description { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public string CurrencyCode { get; set; }
        public decimal ExchangeRate { get; set; } = 1;
        public string Reference { get; set; }
    }

    public class UpdateJournalEntryDto : CreateJournalEntryDto
    {
        public Guid Id { get; set; }
    }

    public class PostJournalEntryDto
    {
        public Guid JournalEntryId { get; set; }
    }

    public class ReverseJournalEntryDto
    {
        public Guid JournalEntryId { get; set; }
        public DateTime ReverseDate { get; set; }
        public string Description { get; set; }
    }

    // ─── Financial Reports ────────────────────────────────────────────────────────

    public class ReportFilterDto
    {
        public Guid CompanyId { get; set; }
        public Guid? FiscalYearId { get; set; }
        public int? StartPeriod { get; set; }
        public int? EndPeriod { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IncludeZeroBalances { get; set; } = false;
        public bool Comparative { get; set; } = false;
    }

    public class TrialBalanceDto
    {
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string FiscalYearName { get; set; }
        public string PeriodRange { get; set; }
        public DateTime AsOf { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public bool IsBalanced { get; set; }
        public List<TrialBalanceLineDto> Lines { get; set; } = new List<TrialBalanceLineDto>();
    }

    public class TrialBalanceLineDto
    {
        public Guid AccountId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string AccountType { get; set; }
        public string AccountSubType { get; set; }
        public string NormalBalance { get; set; }
        public int Level { get; set; }
        public Guid? ParentId { get; set; }
        public decimal OpeningDebit { get; set; }
        public decimal OpeningCredit { get; set; }
        public decimal PeriodDebit { get; set; }
        public decimal PeriodCredit { get; set; }
        public decimal ClosingDebit { get; set; }
        public decimal ClosingCredit { get; set; }
        public decimal Balance { get; set; }        // ClosingDebit - ClosingCredit (for Dr accounts) or reverse
    }

    public class BalanceSheetDto
    {
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string FiscalYearName { get; set; }
        public DateTime AsOf { get; set; }
        public decimal TotalAssets { get; set; }
        public decimal TotalLiabilities { get; set; }
        public decimal TotalEquity { get; set; }
        public decimal TotalLiabilitiesAndEquity { get; set; }
        public bool IsBalanced { get; set; }
        public BalanceSheetSectionDto Assets { get; set; }
        public BalanceSheetSectionDto Liabilities { get; set; }
        public BalanceSheetSectionDto Equity { get; set; }
    }

    public class BalanceSheetSectionDto
    {
        public string Title { get; set; }
        public decimal Total { get; set; }
        public List<BalanceSheetGroupDto> Groups { get; set; } = new List<BalanceSheetGroupDto>();
    }

    public class BalanceSheetGroupDto
    {
        public string SubType { get; set; }
        public decimal Total { get; set; }
        public List<TrialBalanceLineDto> Lines { get; set; } = new List<TrialBalanceLineDto>();
    }

    public class IncomeStatementDto
    {
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string FiscalYearName { get; set; }
        public string PeriodRange { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal NetIncome { get; set; }
        public IncomeStatementSectionDto Revenue { get; set; }
        public IncomeStatementSectionDto CostOfSales { get; set; }
        public IncomeStatementSectionDto OperatingExpenses { get; set; }
        public IncomeStatementSectionDto OtherIncome { get; set; }
        public IncomeStatementSectionDto OtherExpenses { get; set; }
    }

    public class IncomeStatementSectionDto
    {
        public string Title { get; set; }
        public decimal Total { get; set; }
        public List<TrialBalanceLineDto> Lines { get; set; } = new List<TrialBalanceLineDto>();
    }

    // ─── Bank Accounts ────────────────────────────────────────────────────────────

    public class BankAccountDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; }
        public Guid? BranchId { get; set; }
        public string BranchName { get; set; }
        public Guid? GlAccountId { get; set; }
        public string GlAccountCode { get; set; }
        public string GlAccountName { get; set; }
        public string BankName { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public string IBAN { get; set; }
        public string SwiftCode { get; set; }
        public string CurrencyCode { get; set; }
        public decimal OpeningBalance { get; set; }
        public DateTime? OpeningDate { get; set; }
        public decimal CurrentBalance { get; set; }
        public bool IsActive { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateBankAccountDto
    {
        public Guid CompanyId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? GlAccountId { get; set; }
        public string BankName { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public string IBAN { get; set; }
        public string SwiftCode { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public decimal OpeningBalance { get; set; }
        public DateTime? OpeningDate { get; set; }
        public string Notes { get; set; }
    }

    public class UpdateBankAccountDto : CreateBankAccountDto
    {
        public Guid Id { get; set; }
        public bool IsActive { get; set; }
    }

    public class BankReconciliationDto
    {
        public Guid Id { get; set; }
        public Guid BankAccountId { get; set; }
        public string BankAccountName { get; set; }
        public string AccountNumber { get; set; }
        public DateTime StatementDate { get; set; }
        public decimal StatementEndBalance { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClearedDeposits { get; set; }
        public decimal ClearedWithdrawals { get; set; }
        public decimal ClearedBalance { get; set; }
        public decimal Difference { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string CompletedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<BankReconciliationLineDto> Lines { get; set; } = new List<BankReconciliationLineDto>();
    }

    public class BankReconciliationLineDto
    {
        public Guid Id { get; set; }
        public Guid? JournalEntryLineId { get; set; }
        public string EntryNumber { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public bool IsCleared { get; set; }
        public DateTime? ClearedDate { get; set; }
    }

    public class CreateBankReconciliationDto
    {
        public Guid BankAccountId { get; set; }
        public DateTime StatementDate { get; set; }
        public decimal StatementEndBalance { get; set; }
        public string Notes { get; set; }
    }

    public class UpdateReconciliationLineDto
    {
        public Guid ReconciliationLineId { get; set; }
        public bool IsCleared { get; set; }
        public DateTime? ClearedDate { get; set; }
    }
}
