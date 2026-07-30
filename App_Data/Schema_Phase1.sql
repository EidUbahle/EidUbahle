-- =============================================================================
-- EidUbahle Enterprise ERP – Phase 1 Database Schema
-- MSSQL Server 2019+
-- Run this script against a fresh database.
-- =============================================================================

USE EidUbahleDB;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. SaaS / Tenant Foundation
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='saas_SubscriptionPlans')
CREATE TABLE saas_SubscriptionPlans (
    Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    Name                NVARCHAR(100)    NOT NULL,
    Description         NVARCHAR(500),
    PriceMonthly        DECIMAL(18,2)    NOT NULL DEFAULT 0,
    PriceAnnual         DECIMAL(18,2)    NOT NULL DEFAULT 0,
    MaxUsers            INT              NOT NULL DEFAULT 5,
    MaxCompanies        INT              NOT NULL DEFAULT 1,
    MaxBranches         INT              NOT NULL DEFAULT 3,
    StorageLimitBytes   BIGINT           NOT NULL DEFAULT 1073741824,
    TrialDays           INT              NOT NULL DEFAULT 14,
    EnabledModules      NVARCHAR(MAX),   -- JSON array
    IsActive            BIT              NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='saas_Tenants')
CREATE TABLE saas_Tenants (
    Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    Name                NVARCHAR(200)    NOT NULL,
    Subdomain           NVARCHAR(100)    NOT NULL,
    LogoUrl             NVARCHAR(500),
    AccentColor         NVARCHAR(20)     NOT NULL DEFAULT '#2563EB',
    ThemeMode           NVARCHAR(10)     NOT NULL DEFAULT 'light',
    ActiveLayout        NVARCHAR(20)     NOT NULL DEFAULT 'classic',
    SubscriptionPlanId  UNIQUEIDENTIFIER NOT NULL REFERENCES saas_SubscriptionPlans(Id),
    IsActive            BIT              NOT NULL DEFAULT 1,
    TrialEndsAt         DATETIME2,
    DefaultLanguageCode NVARCHAR(10)     NOT NULL DEFAULT 'en',
    DefaultCurrencyCode NVARCHAR(10)     NOT NULL DEFAULT 'USD',
    TimeZone            NVARCHAR(100)    NOT NULL DEFAULT 'UTC',
    MaxUsers            INT              NOT NULL DEFAULT 5,
    MaxCompanies        INT              NOT NULL DEFAULT 1,
    StorageLimitBytes   BIGINT           NOT NULL DEFAULT 1073741824,
    CreatedAt           DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted           BIT              NOT NULL DEFAULT 0,
    DeletedAt           DATETIME2
);
CREATE UNIQUE INDEX UX_Tenants_Subdomain ON saas_Tenants(Subdomain) WHERE IsDeleted=0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='saas_TenantSubscriptions')
CREATE TABLE saas_TenantSubscriptions (
    Id                      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId                UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    PlanId                  UNIQUEIDENTIFIER NOT NULL REFERENCES saas_SubscriptionPlans(Id),
    Status                  NVARCHAR(20)     NOT NULL DEFAULT 'Trial', -- Active|Trial|Expired|Cancelled
    StartDate               DATETIME2        NOT NULL,
    EndDate                 DATETIME2        NOT NULL,
    PaymentProvider         NVARCHAR(50),
    ExternalSubscriptionId  NVARCHAR(200),
    CreatedAt               DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt               DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='saas_Companies')
CREATE TABLE saas_Companies (
    Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId            UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    Name                NVARCHAR(200)    NOT NULL,
    LegalName           NVARCHAR(300),
    RegistrationNumber  NVARCHAR(100),
    TaxNumber           NVARCHAR(100),
    Address             NVARCHAR(500),
    City                NVARCHAR(100),
    Country             NVARCHAR(100),
    Phone               NVARCHAR(50),
    Email               NVARCHAR(200),
    Website             NVARCHAR(300),
    LogoUrl             NVARCHAR(500),
    DefaultCurrencyCode NVARCHAR(10)     NOT NULL DEFAULT 'USD',
    FiscalYearStart     NVARCHAR(10)     NOT NULL DEFAULT '01-01',
    AccountingBasis     NVARCHAR(10)     NOT NULL DEFAULT 'Accrual',
    IsActive            BIT              NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted           BIT              NOT NULL DEFAULT 0,
    DeletedAt           DATETIME2,
    Version             ROWVERSION
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='saas_Branches')
CREATE TABLE saas_Branches (
    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId    UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    CompanyId   UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Companies(Id),
    Name        NVARCHAR(200)    NOT NULL,
    Code        NVARCHAR(20)     NOT NULL,
    Address     NVARCHAR(500),
    Phone       NVARCHAR(50),
    IsHeadOffice BIT             NOT NULL DEFAULT 0,
    IsActive    BIT              NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted   BIT              NOT NULL DEFAULT 0,
    DeletedAt   DATETIME2,
    Version     ROWVERSION
);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. Users, Roles, Permissions
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_Users')
CREATE TABLE sys_Users (
    Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId            UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    Username            NVARCHAR(100)    NOT NULL,
    Email               NVARCHAR(300),
    PasswordHash        NVARCHAR(500),
    PasswordSalt        NVARCHAR(200),
    FullName            NVARCHAR(300),
    AvatarUrl           NVARCHAR(500),
    Phone               NVARCHAR(50),
    LanguageCode        NVARCHAR(10)     NOT NULL DEFAULT 'en',
    ThemeMode           NVARCHAR(10)     NOT NULL DEFAULT 'auto',
    ActiveLayout        NVARCHAR(20),
    AccentColor         NVARCHAR(20),
    IsTenantAdmin       BIT              NOT NULL DEFAULT 0,
    IsSuperAdmin        BIT              NOT NULL DEFAULT 0,
    IsActive            BIT              NOT NULL DEFAULT 1,
    TwoFactorEnabled    BIT              NOT NULL DEFAULT 0,
    TwoFactorSecret     NVARCHAR(200),
    FailedLoginAttempts INT              NOT NULL DEFAULT 0,
    LockedUntil         DATETIME2,
    LastLoginAt         DATETIME2,
    LastSyncAt          DATETIME2,
    CreatedAt           DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted           BIT              NOT NULL DEFAULT 0,
    DeletedAt           DATETIME2,
    Version             ROWVERSION
);
CREATE UNIQUE INDEX UX_Users_Username ON sys_Users(TenantId, Username) WHERE IsDeleted=0;
CREATE UNIQUE INDEX UX_Users_Email    ON sys_Users(TenantId, Email) WHERE IsDeleted=0 AND Email IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_Roles')
CREATE TABLE sys_Roles (
    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId    UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    Name        NVARCHAR(100)    NOT NULL,
    Description NVARCHAR(500),
    IsSystem    BIT              NOT NULL DEFAULT 0,
    IsActive    BIT              NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted   BIT              NOT NULL DEFAULT 0
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_Permissions')
CREATE TABLE sys_Permissions (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    Module          NVARCHAR(100)    NOT NULL,
    Feature         NVARCHAR(100)    NOT NULL,
    Action          NVARCHAR(50)     NOT NULL,
    PermissionKey   NVARCHAR(200)    NOT NULL,
    CONSTRAINT UX_Permissions_Key UNIQUE (PermissionKey)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_RolePermissions')
CREATE TABLE sys_RolePermissions (
    Id           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    RoleId       UNIQUEIDENTIFIER NOT NULL REFERENCES sys_Roles(Id),
    PermissionId UNIQUEIDENTIFIER NOT NULL REFERENCES sys_Permissions(Id),
    IsGranted    BIT              NOT NULL DEFAULT 1,
    CONSTRAINT UX_RolePermissions UNIQUE (RoleId, PermissionId)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_UserRoles')
CREATE TABLE sys_UserRoles (
    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    UserId      UNIQUEIDENTIFIER NOT NULL REFERENCES sys_Users(Id),
    RoleId      UNIQUEIDENTIFIER NOT NULL REFERENCES sys_Roles(Id),
    CompanyId   UNIQUEIDENTIFIER REFERENCES saas_Companies(Id),
    BranchId    UNIQUEIDENTIFIER REFERENCES saas_Branches(Id),
    AssignedAt  DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UX_UserRoles UNIQUE (UserId, RoleId, CompanyId, BranchId)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_UserCompanyBranches')
CREATE TABLE sys_UserCompanyBranches (
    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    UserId      UNIQUEIDENTIFIER NOT NULL REFERENCES sys_Users(Id),
    CompanyId   UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Companies(Id),
    BranchId    UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Branches(Id),
    CONSTRAINT UX_UserCompanyBranch UNIQUE (UserId, CompanyId, BranchId)
);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. Auth / Sessions
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_RefreshTokens')
CREATE TABLE sys_RefreshTokens (
    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    UserId      UNIQUEIDENTIFIER NOT NULL REFERENCES sys_Users(Id),
    Token       NVARCHAR(500)    NOT NULL,
    DeviceId    NVARCHAR(200),
    DeviceInfo  NVARCHAR(500),
    IpAddress   NVARCHAR(50),
    ExpiresAt   DATETIME2        NOT NULL,
    IsRevoked   BIT              NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    INDEX IX_RefreshTokens_Token (Token),
    INDEX IX_RefreshTokens_UserId (UserId)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_LoginHistory')
CREATE TABLE sys_LoginHistory (
    Id            UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    UserId        UNIQUEIDENTIFIER NOT NULL REFERENCES sys_Users(Id),
    IpAddress     NVARCHAR(50),
    UserAgent     NVARCHAR(500),
    Success       BIT              NOT NULL,
    FailureReason NVARCHAR(300),
    AttemptedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    INDEX IX_LoginHistory_UserId (UserId)
);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 4. Localization / Multi-Language
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_Languages')
CREATE TABLE sys_Languages (
    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    Code        NVARCHAR(10)     NOT NULL,
    Name        NVARCHAR(100)    NOT NULL,
    NativeName  NVARCHAR(100),
    Direction   NVARCHAR(5)      NOT NULL DEFAULT 'ltr',
    FlagIcon    NVARCHAR(50),
    IsDefault   BIT              NOT NULL DEFAULT 0,
    IsActive    BIT              NOT NULL DEFAULT 1,
    SortOrder   INT              NOT NULL DEFAULT 100,
    CreatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UX_Languages_Code UNIQUE (Code)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_Translations')
CREATE TABLE sys_Translations (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER REFERENCES saas_Tenants(Id),  -- NULL = system-wide
    LanguageCode    NVARCHAR(10)     NOT NULL,
    TranslationKey  NVARCHAR(500)    NOT NULL,
    [Text]          NVARCHAR(MAX)    NOT NULL,
    Module          NVARCHAR(100)    NOT NULL DEFAULT 'General',
    IsCustom        BIT              NOT NULL DEFAULT 0,
    IsDeleted       BIT              NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    Version         ROWVERSION,
    INDEX IX_Translations_LangKey (LanguageCode, TranslationKey),
    INDEX IX_Translations_Tenant  (TenantId, LanguageCode)
);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 5. Feature Flags
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_FeatureFlags')
CREATE TABLE sys_FeatureFlags (
    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId    UNIQUEIDENTIFIER REFERENCES saas_Tenants(Id),
    FeatureKey  NVARCHAR(200)    NOT NULL,
    IsEnabled   BIT              NOT NULL DEFAULT 0,
    Notes       NVARCHAR(500),
    CreatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    INDEX IX_FeatureFlags_Tenant (TenantId, FeatureKey)
);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 6. Audit Logs (Immutable)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_AuditLogs')
CREATE TABLE sys_AuditLogs (
    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId    UNIQUEIDENTIFIER,
    CompanyId   UNIQUEIDENTIFIER,
    BranchId    UNIQUEIDENTIFIER,
    UserId      UNIQUEIDENTIFIER,
    EntityType  NVARCHAR(100)    NOT NULL,
    EntityId    UNIQUEIDENTIFIER,
    Action      NVARCHAR(50)     NOT NULL,
    OldValueJson NVARCHAR(MAX),
    NewValueJson NVARCHAR(MAX),
    IpAddress   NVARCHAR(50),
    UserAgent   NVARCHAR(500),
    SyncOrigin  NVARCHAR(50),    -- Web | Mobile | Sync | API
    CorrelationId NVARCHAR(100),
    CreatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    INDEX IX_AuditLogs_Tenant   (TenantId, CreatedAt),
    INDEX IX_AuditLogs_Entity   (EntityType, EntityId),
    INDEX IX_AuditLogs_User     (UserId, CreatedAt)
    -- No UPDATE/DELETE permissions should be granted on this table
);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 7. Sync / Offline
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_SyncLogs')
CREATE TABLE sys_SyncLogs (
    Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId            UNIQUEIDENTIFIER NOT NULL,
    UserId              UNIQUEIDENTIFIER NOT NULL,
    DeviceId            NVARCHAR(200),
    SyncType            NVARCHAR(20)     NOT NULL, -- Push|Pull|Full
    Status              NVARCHAR(20)     NOT NULL, -- Success|Failed|Conflict|Partial
    RecordsPushed       INT              NOT NULL DEFAULT 0,
    RecordsPulled       INT              NOT NULL DEFAULT 0,
    ConflictsDetected   INT              NOT NULL DEFAULT 0,
    ConflictsResolved   INT              NOT NULL DEFAULT 0,
    ErrorDetails        NVARCHAR(MAX),
    DurationMs          BIGINT           NOT NULL DEFAULT 0,
    StartedAt           DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CompletedAt         DATETIME2,
    INDEX IX_SyncLogs_Tenant (TenantId, StartedAt)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_SyncConflicts')
CREATE TABLE sys_SyncConflicts (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL,
    EntityType      NVARCHAR(100)    NOT NULL,
    EntityId        UNIQUEIDENTIFIER NOT NULL,
    ClientJson      NVARCHAR(MAX),
    ServerJson      NVARCHAR(MAX),
    Resolution      NVARCHAR(20)     NOT NULL DEFAULT 'Pending',
    ResolvedBy      UNIQUEIDENTIFIER,
    DetectedAt      DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    ResolvedAt      DATETIME2,
    ClientVersion   VARBINARY(8),
    ServerVersion   VARBINARY(8),
    INDEX IX_SyncConflicts_Tenant   (TenantId, Resolution),
    INDEX IX_SyncConflicts_Entity   (EntityType, EntityId)
);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 8. System Settings (hierarchical: System → Company → Branch → User)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_Settings')
CREATE TABLE sys_Settings (
    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId    UNIQUEIDENTIFIER REFERENCES saas_Tenants(Id),
    CompanyId   UNIQUEIDENTIFIER REFERENCES saas_Companies(Id),
    BranchId    UNIQUEIDENTIFIER REFERENCES saas_Branches(Id),
    UserId      UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    SettingKey  NVARCHAR(200)    NOT NULL,
    SettingValue NVARCHAR(MAX),
    DataType    NVARCHAR(20)     NOT NULL DEFAULT 'string',
    UpdatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    INDEX IX_Settings_Lookup (TenantId, CompanyId, BranchId, UserId, SettingKey)
);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SEED DATA
-- ─────────────────────────────────────────────────────────────────────────────

-- Seed default languages
IF NOT EXISTS (SELECT 1 FROM sys_Languages WHERE Code='en')
BEGIN
    INSERT INTO sys_Languages (Id,Code,Name,NativeName,Direction,FlagIcon,IsDefault,IsActive,SortOrder) VALUES
    (NEWID(),'en','English','English','ltr','🇬🇧',1,1,1),
    (NEWID(),'so','Somali','Soomaali','ltr','🇸🇴',0,1,2),
    (NEWID(),'ar','Arabic','العربية','rtl','🇸🇦',0,1,3),
    (NEWID(),'fr','French','Français','ltr','🇫🇷',0,1,4),
    (NEWID(),'sw','Swahili','Kiswahili','ltr','🇰🇪',0,1,5),
    (NEWID(),'es','Spanish','Español','ltr','🇪🇸',0,1,6),
    (NEWID(),'de','German','Deutsch','ltr','🇩🇪',0,1,7),
    (NEWID(),'tr','Turkish','Türkçe','ltr','🇹🇷',0,1,8);
END
GO

-- Seed default subscription plan
IF NOT EXISTS (SELECT 1 FROM saas_SubscriptionPlans WHERE Name='Trial')
BEGIN
    INSERT INTO saas_SubscriptionPlans
        (Id,Name,Description,PriceMonthly,PriceAnnual,MaxUsers,MaxCompanies,MaxBranches,StorageLimitBytes,TrialDays,EnabledModules,IsActive)
    VALUES
        (NEWID(),'Trial','14-day free trial',0,0,3,1,2,536870912,14,'["Accounting","Inventory","Sales","Purchases"]',1),
        (NEWID(),'Starter','For small teams',29,290,10,1,5,2147483648,0,'["Accounting","Inventory","Sales","Purchases","Banking"]',1),
        (NEWID(),'Professional','For growing businesses',79,790,50,3,20,10737418240,0,'["Accounting","Inventory","Sales","Purchases","Banking","CRM","HR","Reports"]',1),
        (NEWID(),'Enterprise','Unlimited everything',299,2990,999,999,999,107374182400,0,'["*"]',1);
END
GO

-- Seed core permissions
IF NOT EXISTS (SELECT 1 FROM sys_Permissions WHERE PermissionKey='accounting.journal.view')
BEGIN
    INSERT INTO sys_Permissions(Id,Module,Feature,Action,PermissionKey) VALUES
    -- Accounting
    (NEWID(),'Accounting','Journal','View','accounting.journal.view'),
    (NEWID(),'Accounting','Journal','Create','accounting.journal.create'),
    (NEWID(),'Accounting','Journal','Edit','accounting.journal.edit'),
    (NEWID(),'Accounting','Journal','Delete','accounting.journal.delete'),
    (NEWID(),'Accounting','Journal','Approve','accounting.journal.approve'),
    (NEWID(),'Accounting','Journal','Post','accounting.journal.post'),
    (NEWID(),'Accounting','ChartOfAccounts','View','accounting.coa.view'),
    (NEWID(),'Accounting','ChartOfAccounts','Create','accounting.coa.create'),
    (NEWID(),'Accounting','ChartOfAccounts','Edit','accounting.coa.edit'),
    (NEWID(),'Accounting','ChartOfAccounts','Delete','accounting.coa.delete'),
    -- Sales
    (NEWID(),'Sales','Invoice','View','sales.invoice.view'),
    (NEWID(),'Sales','Invoice','Create','sales.invoice.create'),
    (NEWID(),'Sales','Invoice','Edit','sales.invoice.edit'),
    (NEWID(),'Sales','Invoice','Delete','sales.invoice.delete'),
    (NEWID(),'Sales','Invoice','Approve','sales.invoice.approve'),
    (NEWID(),'Sales','Invoice','Export','sales.invoice.export'),
    -- Inventory
    (NEWID(),'Inventory','Product','View','inventory.product.view'),
    (NEWID(),'Inventory','Product','Create','inventory.product.create'),
    (NEWID(),'Inventory','Product','Edit','inventory.product.edit'),
    (NEWID(),'Inventory','Product','Delete','inventory.product.delete'),
    -- Admin
    (NEWID(),'Admin','Users','View','admin.users.view'),
    (NEWID(),'Admin','Users','Create','admin.users.create'),
    (NEWID(),'Admin','Users','Edit','admin.users.edit'),
    (NEWID(),'Admin','Users','Delete','admin.users.delete'),
    (NEWID(),'Admin','Translations','View','admin.translations.view'),
    (NEWID(),'Admin','Translations','Edit','admin.translations.edit'),
    (NEWID(),'Admin','Translations','Import','admin.translations.import'),
    (NEWID(),'Admin','Translations','Export','admin.translations.export'),
    (NEWID(),'Admin','Settings','View','admin.settings.view'),
    (NEWID(),'Admin','Settings','Edit','admin.settings.edit'),
    -- Reports
    (NEWID(),'Reports','TrialBalance','View','reports.trialbalance.view'),
    (NEWID(),'Reports','ProfitLoss','View','reports.profitloss.view'),
    (NEWID(),'Reports','BalanceSheet','View','reports.balancesheet.view'),
    (NEWID(),'Reports','GeneralLedger','View','reports.gl.view'),
    (NEWID(),'Reports','Export','Export','reports.export');
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- STORED PROCEDURES
-- ─────────────────────────────────────────────────────────────────────────────

-- sp_Translation_Upsert
IF OBJECT_ID('sp_Translation_Upsert','P') IS NOT NULL DROP PROCEDURE sp_Translation_Upsert;
GO
CREATE PROCEDURE sp_Translation_Upsert
    @Id             UNIQUEIDENTIFIER,
    @TenantId       UNIQUEIDENTIFIER,
    @LanguageCode   NVARCHAR(10),
    @TranslationKey NVARCHAR(500),
    @Text           NVARCHAR(MAX),
    @Module         NVARCHAR(100),
    @IsCustom       BIT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM sys_Translations
               WHERE LanguageCode=@LanguageCode AND TranslationKey=@TranslationKey
                 AND ISNULL(TenantId,'00000000-0000-0000-0000-000000000000')
                     = ISNULL(@TenantId,'00000000-0000-0000-0000-000000000000'))
    BEGIN
        UPDATE sys_Translations
        SET [Text]=@Text, Module=@Module, IsCustom=@IsCustom, UpdatedAt=GETUTCDATE()
        WHERE LanguageCode=@LanguageCode AND TranslationKey=@TranslationKey
          AND ISNULL(TenantId,'00000000-0000-0000-0000-000000000000')
              = ISNULL(@TenantId,'00000000-0000-0000-0000-000000000000');
    END
    ELSE
    BEGIN
        INSERT INTO sys_Translations(Id,TenantId,LanguageCode,TranslationKey,[Text],Module,IsCustom)
        VALUES(@Id,@TenantId,@LanguageCode,@TranslationKey,@Text,@Module,@IsCustom);
    END
END
GO

-- sp_Language_Upsert
IF OBJECT_ID('sp_Language_Upsert','P') IS NOT NULL DROP PROCEDURE sp_Language_Upsert;
GO
CREATE PROCEDURE sp_Language_Upsert
    @Id         UNIQUEIDENTIFIER,
    @Code       NVARCHAR(10),
    @Name       NVARCHAR(100),
    @NativeName NVARCHAR(100),
    @Direction  NVARCHAR(5),
    @FlagIcon   NVARCHAR(50),
    @IsDefault  BIT,
    @IsActive   BIT,
    @SortOrder  INT
AS
BEGIN
    SET NOCOUNT ON;
    IF @IsDefault = 1
        UPDATE sys_Languages SET IsDefault=0;  -- only one default

    IF EXISTS (SELECT 1 FROM sys_Languages WHERE Code=@Code)
        UPDATE sys_Languages SET Name=@Name,NativeName=@NativeName,Direction=@Direction,
               FlagIcon=@FlagIcon,IsDefault=@IsDefault,IsActive=@IsActive,SortOrder=@SortOrder
        WHERE Code=@Code;
    ELSE
        INSERT INTO sys_Languages(Id,Code,Name,NativeName,Direction,FlagIcon,IsDefault,IsActive,SortOrder)
        VALUES(@Id,@Code,@Name,@NativeName,@Direction,@FlagIcon,@IsDefault,@IsActive,@SortOrder);
END
GO

-- sp_AuditLog_Insert (append-only, no updates via app)
IF OBJECT_ID('sp_AuditLog_Insert','P') IS NOT NULL DROP PROCEDURE sp_AuditLog_Insert;
GO
CREATE PROCEDURE sp_AuditLog_Insert
    @TenantId       UNIQUEIDENTIFIER,
    @CompanyId      UNIQUEIDENTIFIER,
    @BranchId       UNIQUEIDENTIFIER,
    @UserId         UNIQUEIDENTIFIER,
    @EntityType     NVARCHAR(100),
    @EntityId       UNIQUEIDENTIFIER,
    @Action         NVARCHAR(50),
    @OldValueJson   NVARCHAR(MAX),
    @NewValueJson   NVARCHAR(MAX),
    @IpAddress      NVARCHAR(50),
    @UserAgent      NVARCHAR(500),
    @SyncOrigin     NVARCHAR(50),
    @CorrelationId  NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO sys_AuditLogs(Id,TenantId,CompanyId,BranchId,UserId,EntityType,EntityId,
                              Action,OldValueJson,NewValueJson,IpAddress,UserAgent,SyncOrigin,CorrelationId)
    VALUES(NEWID(),@TenantId,@CompanyId,@BranchId,@UserId,@EntityType,@EntityId,
           @Action,@OldValueJson,@NewValueJson,@IpAddress,@UserAgent,@SyncOrigin,@CorrelationId);
END
GO

-- sp_Sync_Push (process sync push from offline device)
IF OBJECT_ID('sp_Sync_LogEntry','P') IS NOT NULL DROP PROCEDURE sp_Sync_LogEntry;
GO
CREATE PROCEDURE sp_Sync_LogEntry
    @TenantId           UNIQUEIDENTIFIER,
    @UserId             UNIQUEIDENTIFIER,
    @DeviceId           NVARCHAR(200),
    @SyncType           NVARCHAR(20),
    @Status             NVARCHAR(20),
    @RecordsPushed      INT,
    @RecordsPulled      INT,
    @ConflictsDetected  INT,
    @ConflictsResolved  INT,
    @ErrorDetails       NVARCHAR(MAX),
    @DurationMs         BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO sys_SyncLogs(Id,TenantId,UserId,DeviceId,SyncType,Status,RecordsPushed,
                             RecordsPulled,ConflictsDetected,ConflictsResolved,ErrorDetails,DurationMs,CompletedAt)
    VALUES(NEWID(),@TenantId,@UserId,@DeviceId,@SyncType,@Status,@RecordsPushed,
           @RecordsPulled,@ConflictsDetected,@ConflictsResolved,@ErrorDetails,@DurationMs,GETUTCDATE());
END
GO
