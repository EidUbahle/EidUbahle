-- ============================================================
-- Script: V003__Identity_Core.sql
-- Description: Creates the core Identity tables for Phase 2/3:
--              IdentityUsers, IdentityApplications,
--              IdentityUserApplications and
--              IdentityAuthorizationCodes (OAuth2/OIDC).
--              Idempotent: safe to re-run.
-- ============================================================

USE CentralIdentityDb;
GO

-- ============================================================
-- IdentityUsers
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[IdentityUsers]')
      AND type = 'U'
)
BEGIN
    CREATE TABLE [dbo].[IdentityUsers] (
        [UserId]                BIGINT          NOT NULL IDENTITY(1,1),
        [Username]              NVARCHAR(100)   NOT NULL,
        [Email]                 NVARCHAR(256)   NOT NULL,
        [Phone]                 NVARCHAR(50)    NULL,
        [PasswordHash]          NVARCHAR(512)   NOT NULL,
        [FirstName]             NVARCHAR(100)   NOT NULL,
        [LastName]              NVARCHAR(100)   NOT NULL,
        [IsActive]              BIT             NOT NULL DEFAULT 1,
        [EmailVerified]         BIT             NOT NULL DEFAULT 0,
        [PhoneVerified]         BIT             NOT NULL DEFAULT 0,
        [TwoFactorEnabled]      BIT             NOT NULL DEFAULT 0,
        [FailedLoginAttempts]   INT             NOT NULL DEFAULT 0,
        [LockoutEndUtc]         DATETIME2(7)    NULL,
        [PasswordChangedAtUtc]  DATETIME2(7)    NULL,
        [LastLoginAtUtc]        DATETIME2(7)    NULL,
        [SecurityStamp]         NVARCHAR(64)    NOT NULL,
        [CreatedAtUtc]          DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAtUtc]          DATETIME2(7)    NULL,
        CONSTRAINT [PK_IdentityUsers] PRIMARY KEY CLUSTERED ([UserId] ASC),
        CONSTRAINT [UQ_IdentityUsers_Username] UNIQUE ([Username]),
        CONSTRAINT [UQ_IdentityUsers_Email] UNIQUE ([Email])
    );
END
GO

-- ============================================================
-- IdentityApplications
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[IdentityApplications]')
      AND type = 'U'
)
BEGIN
    CREATE TABLE [dbo].[IdentityApplications] (
        [ApplicationId]         BIGINT          NOT NULL IDENTITY(1,1),
        [ApplicationCode]       NVARCHAR(50)    NOT NULL,
        [ApplicationName]       NVARCHAR(200)   NOT NULL,
        [Description]           NVARCHAR(1000)  NULL,
        [ClientId]              NVARCHAR(128)   NOT NULL,
        [ClientSecretHash]      NVARCHAR(512)   NULL,
        [ClientType]            NVARCHAR(20)    NOT NULL DEFAULT 'Confidential',
        [Audience]              NVARCHAR(200)   NOT NULL,
        [AllowedRedirectUris]   NVARCHAR(MAX)   NULL,
        [AllowedOrigins]        NVARCHAR(MAX)   NULL,
        [IsActive]              BIT             NOT NULL DEFAULT 1,
        [CreatedAtUtc]          DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAtUtc]          DATETIME2(7)    NULL,
        CONSTRAINT [PK_IdentityApplications] PRIMARY KEY CLUSTERED ([ApplicationId] ASC),
        CONSTRAINT [UQ_IdentityApplications_ApplicationCode] UNIQUE ([ApplicationCode]),
        CONSTRAINT [UQ_IdentityApplications_ClientId] UNIQUE ([ClientId])
    );
END
GO

-- ============================================================
-- IdentityUserApplications
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[IdentityUserApplications]')
      AND type = 'U'
)
BEGIN
    CREATE TABLE [dbo].[IdentityUserApplications] (
        [UserApplicationId]     BIGINT          NOT NULL IDENTITY(1,1),
        [UserId]                BIGINT          NOT NULL,
        [ApplicationId]         BIGINT          NOT NULL,
        [IsActive]              BIT             NOT NULL DEFAULT 1,
        [AssignedAtUtc]         DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
        [LastAccessAtUtc]       DATETIME2(7)    NULL,
        [LastActivityAtUtc]     DATETIME2(7)    NULL,
        [RevokedAtUtc]          DATETIME2(7)    NULL,
        [RevocationReason]      NVARCHAR(500)   NULL,
        [SecurityStamp]         NVARCHAR(64)    NOT NULL,
        CONSTRAINT [PK_IdentityUserApplications] PRIMARY KEY CLUSTERED ([UserApplicationId] ASC),
        CONSTRAINT [UQ_IdentityUserApplications_User_App] UNIQUE ([UserId], [ApplicationId]),
        CONSTRAINT [FK_UserApplications_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[IdentityUsers]([UserId]),
        CONSTRAINT [FK_UserApplications_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[IdentityApplications]([ApplicationId])
    );
END
GO

-- ============================================================
-- IdentityAuthorizationCodes (OAuth2 / OIDC authorization_code grant)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[IdentityAuthorizationCodes]')
      AND type = 'U'
)
BEGIN
    CREATE TABLE [dbo].[IdentityAuthorizationCodes] (
        [CodeId]                BIGINT          NOT NULL IDENTITY(1,1),
        [CodeHash]              NVARCHAR(512)   NOT NULL,
        [UserId]                BIGINT          NOT NULL,
        [ApplicationId]         BIGINT          NOT NULL,
        [RedirectUri]           NVARCHAR(2000)  NOT NULL,
        [ClientId]              NVARCHAR(128)   NOT NULL,
        [Scope]                 NVARCHAR(500)   NOT NULL,
        [CodeChallenge]         NVARCHAR(256)   NULL,
        [CodeChallengeMethod]   NVARCHAR(10)    NULL,
        [IsUsed]                BIT             NOT NULL DEFAULT 0,
        [CreatedAtUtc]          DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
        [ExpiresAtUtc]          DATETIME2(7)    NOT NULL,
        CONSTRAINT [PK_IdentityAuthorizationCodes] PRIMARY KEY CLUSTERED ([CodeId] ASC),
        CONSTRAINT [UQ_IdentityAuthorizationCodes_CodeHash] UNIQUE ([CodeHash])
    );
END
GO

-- ============================================================
-- Additional (non-unique) indexes
-- Unique constraints above already create unique indexes for
-- Username, Email, ApplicationCode, ClientId and CodeHash.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IdentityApplications_Audience' AND object_id = OBJECT_ID(N'[dbo].[IdentityApplications]'))
BEGIN
    CREATE INDEX [IX_IdentityApplications_Audience] ON [dbo].[IdentityApplications]([Audience]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IdentityUserApplications_UserId' AND object_id = OBJECT_ID(N'[dbo].[IdentityUserApplications]'))
BEGIN
    CREATE INDEX [IX_IdentityUserApplications_UserId] ON [dbo].[IdentityUserApplications]([UserId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IdentityUserApplications_ApplicationId' AND object_id = OBJECT_ID(N'[dbo].[IdentityUserApplications]'))
BEGIN
    CREATE INDEX [IX_IdentityUserApplications_ApplicationId] ON [dbo].[IdentityUserApplications]([ApplicationId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IdentityAuthorizationCodes_ExpiresAtUtc' AND object_id = OBJECT_ID(N'[dbo].[IdentityAuthorizationCodes]'))
BEGIN
    CREATE INDEX [IX_IdentityAuthorizationCodes_ExpiresAtUtc] ON [dbo].[IdentityAuthorizationCodes]([ExpiresAtUtc]);
END
GO
