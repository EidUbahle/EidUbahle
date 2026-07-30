-- =============================================================================
-- EidUbahle Enterprise ERP – Phase 3 Database Schema
-- Core Accounting Engine: COA, Journal Entries, Fiscal Periods, Banking, Currencies
-- Run this script against EidUbahleDB (after Schema_Phase2.sql).
-- =============================================================================

USE EidUbahleDB;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. Currencies
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='acc_Currencies')
CREATE TABLE acc_Currencies (
    Code            NVARCHAR(10)     NOT NULL PRIMARY KEY,
    Name            NVARCHAR(100)    NOT NULL,
    Symbol          NVARCHAR(10)     NOT NULL,
    DecimalPlaces   INT              NOT NULL DEFAULT 2,
    IsBaseCurrency  BIT              NOT NULL DEFAULT 0,
    IsActive        BIT              NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. Exchange Rates
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='acc_ExchangeRates')
CREATE TABLE acc_ExchangeRates (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    FromCurrency    NVARCHAR(10)     NOT NULL REFERENCES acc_Currencies(Code),
    ToCurrency      NVARCHAR(10)     NOT NULL REFERENCES acc_Currencies(Code),
    Rate            DECIMAL(20,8)    NOT NULL,
    EffectiveDate   DATE             NOT NULL,
    Source          NVARCHAR(50),    -- Manual | ECB | OpenExchange
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy       UNIQUEIDENTIFIER REFERENCES sys_Users(Id)
);
GO
CREATE INDEX IX_ExchangeRates_TenantDate  ON acc_ExchangeRates(TenantId, EffectiveDate DESC);
CREATE INDEX IX_ExchangeRates_CurrencyPair ON acc_ExchangeRates(TenantId, FromCurrency, ToCurrency, EffectiveDate DESC);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. Fiscal Years
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='acc_FiscalYears')
CREATE TABLE acc_FiscalYears (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    CompanyId       UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Companies(Id),
    Year            INT              NOT NULL,
    StartDate       DATE             NOT NULL,
    EndDate         DATE             NOT NULL,
    Status          NVARCHAR(20)     NOT NULL DEFAULT 'Open', -- Open | Closed | Locked
    ClosedAt        DATETIME2,
    ClosedBy        UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy       UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    CONSTRAINT UQ_FiscalYear UNIQUE (CompanyId, Year)
);
GO
CREATE INDEX IX_FiscalYears_TenantId  ON acc_FiscalYears(TenantId);
CREATE INDEX IX_FiscalYears_CompanyId ON acc_FiscalYears(CompanyId);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 4. Fiscal Periods (months within a fiscal year)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='acc_FiscalPeriods')
CREATE TABLE acc_FiscalPeriods (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    CompanyId       UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Companies(Id),
    FiscalYearId    UNIQUEIDENTIFIER NOT NULL REFERENCES acc_FiscalYears(Id),
    PeriodNumber    INT              NOT NULL,  -- 1–12 (or 13 for adjustments)
    Name            NVARCHAR(50)     NOT NULL,  -- e.g. "Jan 2025"
    StartDate       DATE             NOT NULL,
    EndDate         DATE             NOT NULL,
    Status          NVARCHAR(20)     NOT NULL DEFAULT 'Open', -- Open | Closed | Locked
    ClosedAt        DATETIME2,
    ClosedBy        UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_FiscalPeriod UNIQUE (FiscalYearId, PeriodNumber)
);
GO
CREATE INDEX IX_FiscalPeriods_CompanyId ON acc_FiscalPeriods(CompanyId, Status);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 5. Chart of Accounts
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='acc_Accounts')
CREATE TABLE acc_Accounts (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    CompanyId       UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Companies(Id),
    ParentId        UNIQUEIDENTIFIER REFERENCES acc_Accounts(Id),
    Code            NVARCHAR(30)     NOT NULL,
    Name            NVARCHAR(200)    NOT NULL,
    AccountType     NVARCHAR(30)     NOT NULL,  -- Asset | Liability | Equity | Revenue | Expense
    AccountSubType  NVARCHAR(50),               -- CurrentAsset | FixedAsset | CurrentLiab | etc.
    NormalBalance   NVARCHAR(6)      NOT NULL DEFAULT 'Debit',  -- Debit | Credit
    CurrencyCode    NVARCHAR(10)     REFERENCES acc_Currencies(Code),  -- NULL = base currency
    IsPosting       BIT              NOT NULL DEFAULT 1,   -- 1=leaf/posting, 0=header/summary
    IsActive        BIT              NOT NULL DEFAULT 1,
    Description     NVARCHAR(500),
    Level           INT              NOT NULL DEFAULT 1,   -- depth in tree (1=root)
    SortOrder       INT              NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted       BIT              NOT NULL DEFAULT 0,
    DeletedAt       DATETIME2,
    CreatedBy       UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    UpdatedBy       UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    CONSTRAINT UQ_Account_Code UNIQUE (CompanyId, Code)
);
GO
CREATE INDEX IX_Accounts_TenantId   ON acc_Accounts(TenantId) WHERE IsDeleted=0;
CREATE INDEX IX_Accounts_CompanyId  ON acc_Accounts(CompanyId) WHERE IsDeleted=0;
CREATE INDEX IX_Accounts_ParentId   ON acc_Accounts(ParentId) WHERE IsDeleted=0;
CREATE INDEX IX_Accounts_Type       ON acc_Accounts(CompanyId, AccountType) WHERE IsDeleted=0;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 6. Journal Entries (headers)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='acc_JournalEntries')
CREATE TABLE acc_JournalEntries (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    CompanyId       UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Companies(Id),
    BranchId        UNIQUEIDENTIFIER REFERENCES saas_Branches(Id),
    FiscalPeriodId  UNIQUEIDENTIFIER NOT NULL REFERENCES acc_FiscalPeriods(Id),
    EntryNumber     NVARCHAR(30)     NOT NULL,  -- auto-generated: JE-2025-000001
    EntryDate       DATE             NOT NULL,
    Description     NVARCHAR(500)    NOT NULL,
    Reference       NVARCHAR(100),              -- invoice ref, receipt number, etc.
    EntryType       NVARCHAR(20)     NOT NULL DEFAULT 'Normal',  -- Normal | Reversing | Adjustment | Opening | Closing
    CurrencyCode    NVARCHAR(10)     NOT NULL REFERENCES acc_Currencies(Code),
    ExchangeRate    DECIMAL(20,8)    NOT NULL DEFAULT 1,
    TotalDebit      DECIMAL(20,4)    NOT NULL DEFAULT 0,
    TotalCredit     DECIMAL(20,4)    NOT NULL DEFAULT 0,
    BaseCurrencyDebit  DECIMAL(20,4) NOT NULL DEFAULT 0,
    BaseCurrencyCredit DECIMAL(20,4) NOT NULL DEFAULT 0,
    Status          NVARCHAR(20)     NOT NULL DEFAULT 'Draft',  -- Draft | Posted | Reversed | Void
    PostedAt        DATETIME2,
    PostedBy        UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    ReversedEntryId UNIQUEIDENTIFIER REFERENCES acc_JournalEntries(Id),
    Notes           NVARCHAR(1000),
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted       BIT              NOT NULL DEFAULT 0,
    CreatedBy       UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    UpdatedBy       UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    Version         ROWVERSION,
    CONSTRAINT UQ_JournalEntry_Number UNIQUE (CompanyId, EntryNumber)
);
GO
CREATE INDEX IX_JournalEntries_TenantId    ON acc_JournalEntries(TenantId) WHERE IsDeleted=0;
CREATE INDEX IX_JournalEntries_CompanyId   ON acc_JournalEntries(CompanyId, EntryDate DESC) WHERE IsDeleted=0;
CREATE INDEX IX_JournalEntries_PeriodId    ON acc_JournalEntries(FiscalPeriodId) WHERE IsDeleted=0;
CREATE INDEX IX_JournalEntries_Status      ON acc_JournalEntries(CompanyId, Status) WHERE IsDeleted=0;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 7. Journal Entry Lines
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='acc_JournalEntryLines')
CREATE TABLE acc_JournalEntryLines (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    JournalEntryId  UNIQUEIDENTIFIER NOT NULL REFERENCES acc_JournalEntries(Id) ON DELETE CASCADE,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    CompanyId       UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Companies(Id),
    AccountId       UNIQUEIDENTIFIER NOT NULL REFERENCES acc_Accounts(Id),
    LineNumber      INT              NOT NULL,
    Description     NVARCHAR(300),
    DebitAmount     DECIMAL(20,4)    NOT NULL DEFAULT 0,
    CreditAmount    DECIMAL(20,4)    NOT NULL DEFAULT 0,
    CurrencyCode    NVARCHAR(10)     NOT NULL REFERENCES acc_Currencies(Code),
    ExchangeRate    DECIMAL(20,8)    NOT NULL DEFAULT 1,
    BaseDebit       DECIMAL(20,4)    NOT NULL DEFAULT 0,  -- in base currency
    BaseCredit      DECIMAL(20,4)    NOT NULL DEFAULT 0,  -- in base currency
    Reference       NVARCHAR(100),
    Tags            NVARCHAR(500),   -- JSON array for custom tags
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);
GO
CREATE INDEX IX_JELines_JournalEntryId ON acc_JournalEntryLines(JournalEntryId);
CREATE INDEX IX_JELines_AccountId      ON acc_JournalEntryLines(AccountId);
CREATE INDEX IX_JELines_TenantId       ON acc_JournalEntryLines(TenantId, CompanyId);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 8. Bank Accounts
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='acc_BankAccounts')
CREATE TABLE acc_BankAccounts (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    CompanyId       UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Companies(Id),
    BranchId        UNIQUEIDENTIFIER REFERENCES saas_Branches(Id),
    GlAccountId     UNIQUEIDENTIFIER REFERENCES acc_Accounts(Id),  -- linked GL account
    BankName        NVARCHAR(200)    NOT NULL,
    AccountName     NVARCHAR(200)    NOT NULL,
    AccountNumber   NVARCHAR(50)     NOT NULL,
    IBAN            NVARCHAR(50),
    SwiftCode       NVARCHAR(20),
    CurrencyCode    NVARCHAR(10)     NOT NULL REFERENCES acc_Currencies(Code),
    OpeningBalance  DECIMAL(20,4)    NOT NULL DEFAULT 0,
    OpeningDate     DATE,
    CurrentBalance  DECIMAL(20,4)    NOT NULL DEFAULT 0,
    IsActive        BIT              NOT NULL DEFAULT 1,
    Notes           NVARCHAR(500),
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted       BIT              NOT NULL DEFAULT 0,
    DeletedAt       DATETIME2,
    CreatedBy       UNIQUEIDENTIFIER REFERENCES sys_Users(Id)
);
GO
CREATE INDEX IX_BankAccounts_TenantId   ON acc_BankAccounts(TenantId) WHERE IsDeleted=0;
CREATE INDEX IX_BankAccounts_CompanyId  ON acc_BankAccounts(CompanyId) WHERE IsDeleted=0;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 9. Bank Reconciliations
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='acc_BankReconciliations')
CREATE TABLE acc_BankReconciliations (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    BankAccountId   UNIQUEIDENTIFIER NOT NULL REFERENCES acc_BankAccounts(Id),
    StatementDate   DATE             NOT NULL,
    StatementEndBalance  DECIMAL(20,4) NOT NULL DEFAULT 0,
    OpeningBalance  DECIMAL(20,4)    NOT NULL DEFAULT 0,
    ClearedDeposits DECIMAL(20,4)    NOT NULL DEFAULT 0,
    ClearedWithdrawals DECIMAL(20,4) NOT NULL DEFAULT 0,
    ClearedBalance  DECIMAL(20,4)    NOT NULL DEFAULT 0,
    Difference      DECIMAL(20,4)    NOT NULL DEFAULT 0,
    Status          NVARCHAR(20)     NOT NULL DEFAULT 'InProgress', -- InProgress | Completed | Approved
    Notes           NVARCHAR(500),
    CompletedAt     DATETIME2,
    CompletedBy     UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy       UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    CONSTRAINT UQ_BankRecon UNIQUE (BankAccountId, StatementDate)
);
GO
CREATE INDEX IX_BankRecon_BankAccountId ON acc_BankReconciliations(BankAccountId);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 10. Bank Reconciliation Lines
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='acc_BankReconciliationLines')
CREATE TABLE acc_BankReconciliationLines (
    Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    ReconciliationId    UNIQUEIDENTIFIER NOT NULL REFERENCES acc_BankReconciliations(Id) ON DELETE CASCADE,
    JournalEntryLineId  UNIQUEIDENTIFIER REFERENCES acc_JournalEntryLines(Id),
    TenantId            UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    TransactionDate     DATE             NOT NULL,
    Description         NVARCHAR(300),
    DebitAmount         DECIMAL(20,4)    NOT NULL DEFAULT 0,
    CreditAmount        DECIMAL(20,4)    NOT NULL DEFAULT 0,
    IsCleared           BIT              NOT NULL DEFAULT 0,
    ClearedDate         DATE,
    CreatedAt           DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);
GO
CREATE INDEX IX_BankReconLines_ReconciliationId ON acc_BankReconciliationLines(ReconciliationId);
CREATE INDEX IX_BankReconLines_JELineId         ON acc_BankReconciliationLines(JournalEntryLineId) WHERE JournalEntryLineId IS NOT NULL;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 11. Account Balances (materialized cache for performance)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='acc_AccountBalances')
CREATE TABLE acc_AccountBalances (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    CompanyId       UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Companies(Id),
    AccountId       UNIQUEIDENTIFIER NOT NULL REFERENCES acc_Accounts(Id),
    FiscalPeriodId  UNIQUEIDENTIFIER NOT NULL REFERENCES acc_FiscalPeriods(Id),
    OpeningDebit    DECIMAL(20,4)    NOT NULL DEFAULT 0,
    OpeningCredit   DECIMAL(20,4)    NOT NULL DEFAULT 0,
    PeriodDebit     DECIMAL(20,4)    NOT NULL DEFAULT 0,
    PeriodCredit    DECIMAL(20,4)    NOT NULL DEFAULT 0,
    ClosingDebit    DECIMAL(20,4)    NOT NULL DEFAULT 0,
    ClosingCredit   DECIMAL(20,4)    NOT NULL DEFAULT 0,
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_AccountBalance UNIQUE (AccountId, FiscalPeriodId)
);
GO
CREATE INDEX IX_AccountBalances_AccountId  ON acc_AccountBalances(AccountId);
CREATE INDEX IX_AccountBalances_PeriodId   ON acc_AccountBalances(FiscalPeriodId);
CREATE INDEX IX_AccountBalances_CompanyId  ON acc_AccountBalances(CompanyId);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 12. Stored Procedures
-- ─────────────────────────────────────────────────────────────────────────────

-- sp_PostJournalEntry: validates double-entry balance and posts
IF OBJECT_ID('dbo.sp_PostJournalEntry') IS NOT NULL DROP PROCEDURE dbo.sp_PostJournalEntry;
GO
CREATE PROCEDURE dbo.sp_PostJournalEntry
    @JournalEntryId UNIQUEIDENTIFIER,
    @PostedBy       UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        -- Check balance (sum debits = sum credits)
        DECLARE @TotalDebit  DECIMAL(20,4);
        DECLARE @TotalCredit DECIMAL(20,4);
        SELECT @TotalDebit  = ISNULL(SUM(DebitAmount), 0),
               @TotalCredit = ISNULL(SUM(CreditAmount), 0)
        FROM acc_JournalEntryLines
        WHERE JournalEntryId = @JournalEntryId;

        IF ABS(@TotalDebit - @TotalCredit) > 0.005
        BEGIN
            RAISERROR('Journal entry is not balanced: Debit (%s) <> Credit (%s)', 16, 1, @TotalDebit, @TotalCredit);
            ROLLBACK; RETURN;
        END;

        -- Check at least 2 lines
        DECLARE @LineCount INT;
        SELECT @LineCount = COUNT(*) FROM acc_JournalEntryLines WHERE JournalEntryId = @JournalEntryId;
        IF @LineCount < 2
        BEGIN
            RAISERROR('Journal entry must have at least 2 lines', 16, 1);
            ROLLBACK; RETURN;
        END;

        -- Check status is Draft
        IF NOT EXISTS (SELECT 1 FROM acc_JournalEntries WHERE Id = @JournalEntryId AND Status = 'Draft')
        BEGIN
            RAISERROR('Only Draft journal entries can be posted', 16, 1);
            ROLLBACK; RETURN;
        END;

        -- Update header
        UPDATE acc_JournalEntries
        SET Status        = 'Posted',
            TotalDebit    = @TotalDebit,
            TotalCredit   = @TotalCredit,
            PostedAt      = GETUTCDATE(),
            PostedBy      = @PostedBy,
            UpdatedAt     = GETUTCDATE()
        WHERE Id = @JournalEntryId;

        -- Update account balance cache
        DECLARE @FiscalPeriodId UNIQUEIDENTIFIER, @TenantId UNIQUEIDENTIFIER, @CompanyId UNIQUEIDENTIFIER;
        SELECT @FiscalPeriodId = FiscalPeriodId, @TenantId = TenantId, @CompanyId = CompanyId
        FROM acc_JournalEntries WHERE Id = @JournalEntryId;

        -- Upsert into acc_AccountBalances for each line
        MERGE acc_AccountBalances AS target
        USING (
            SELECT AccountId,
                   SUM(BaseDebit)  AS Debit,
                   SUM(BaseCredit) AS Credit
            FROM acc_JournalEntryLines
            WHERE JournalEntryId = @JournalEntryId
            GROUP BY AccountId
        ) AS source ON target.AccountId = source.AccountId
                    AND target.FiscalPeriodId = @FiscalPeriodId
        WHEN MATCHED THEN
            UPDATE SET PeriodDebit   = PeriodDebit  + source.Debit,
                       PeriodCredit  = PeriodCredit + source.Credit,
                       ClosingDebit  = OpeningDebit  + PeriodDebit  + source.Debit,
                       ClosingCredit = OpeningCredit + PeriodCredit + source.Credit,
                       UpdatedAt     = GETUTCDATE()
        WHEN NOT MATCHED THEN
            INSERT (Id, TenantId, CompanyId, AccountId, FiscalPeriodId,
                    OpeningDebit, OpeningCredit, PeriodDebit, PeriodCredit, ClosingDebit, ClosingCredit, UpdatedAt)
            VALUES (NEWID(), @TenantId, @CompanyId, source.AccountId, @FiscalPeriodId,
                    0, 0, source.Debit, source.Credit, source.Debit, source.Credit, GETUTCDATE());

        COMMIT;
        SELECT 1 AS Success, '' AS ErrorMessage;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        SELECT 0 AS Success, ERROR_MESSAGE() AS ErrorMessage;
    END CATCH;
END;
GO

-- sp_GenerateJournalEntryNumber: generates sequential entry numbers per company/year
IF OBJECT_ID('dbo.sp_GenerateJournalEntryNumber') IS NOT NULL DROP PROCEDURE dbo.sp_GenerateJournalEntryNumber;
GO
CREATE PROCEDURE dbo.sp_GenerateJournalEntryNumber
    @CompanyId  UNIQUEIDENTIFIER,
    @EntryDate  DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Year     INT = YEAR(@EntryDate);
    DECLARE @MaxNum   INT;
    DECLARE @Prefix   NVARCHAR(20) = 'JE-' + CAST(@Year AS NVARCHAR) + '-';

    SELECT @MaxNum = ISNULL(MAX(CAST(SUBSTRING(EntryNumber, LEN(@Prefix)+1, 10) AS INT)), 0)
    FROM acc_JournalEntries
    WHERE CompanyId = @CompanyId AND EntryNumber LIKE @Prefix + '%';

    SELECT @Prefix + RIGHT('000000' + CAST(@MaxNum + 1 AS NVARCHAR), 6) AS EntryNumber;
END;
GO

-- sp_GetTrialBalance: returns account balances for a period range
IF OBJECT_ID('dbo.sp_GetTrialBalance') IS NOT NULL DROP PROCEDURE dbo.sp_GetTrialBalance;
GO
CREATE PROCEDURE dbo.sp_GetTrialBalance
    @CompanyId      UNIQUEIDENTIFIER,
    @FiscalYearId   UNIQUEIDENTIFIER,
    @StartPeriod    INT = 1,
    @EndPeriod      INT = 12
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        a.Id         AS AccountId,
        a.Code,
        a.Name,
        a.AccountType,
        a.AccountSubType,
        a.NormalBalance,
        a.Level,
        a.ParentId,
        ISNULL(SUM(ab.OpeningDebit),  0) AS OpeningDebit,
        ISNULL(SUM(ab.OpeningCredit), 0) AS OpeningCredit,
        ISNULL(SUM(ab.PeriodDebit),   0) AS PeriodDebit,
        ISNULL(SUM(ab.PeriodCredit),  0) AS PeriodCredit,
        ISNULL(SUM(ab.ClosingDebit),  0) AS ClosingDebit,
        ISNULL(SUM(ab.ClosingCredit), 0) AS ClosingCredit
    FROM acc_Accounts a
    LEFT JOIN acc_AccountBalances ab ON ab.AccountId = a.Id
    LEFT JOIN acc_FiscalPeriods fp   ON fp.Id = ab.FiscalPeriodId
                                     AND fp.FiscalYearId = @FiscalYearId
                                     AND fp.PeriodNumber BETWEEN @StartPeriod AND @EndPeriod
    WHERE a.CompanyId = @CompanyId
      AND a.IsPosting = 1
      AND a.IsDeleted = 0
      AND a.IsActive = 1
    GROUP BY a.Id, a.Code, a.Name, a.AccountType, a.AccountSubType, a.NormalBalance, a.Level, a.ParentId
    ORDER BY a.Code;
END;
GO

PRINT 'Phase 3 schema applied successfully.';
GO
