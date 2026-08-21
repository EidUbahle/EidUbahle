-- ============================================================
-- Script: V006__MFA.sql
-- Description: MFA tables: IdentityMfaMethods, IdentityRecoveryCodes, IdentityOtpChallenges
-- Idempotent: safe to re-run.
-- ============================================================
USE CentralIdentityDb;
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id=OBJECT_ID(N'[dbo].[IdentityMfaMethods]') AND type='U')
BEGIN
    CREATE TABLE [dbo].[IdentityMfaMethods] (
        [MfaMethodId]         BIGINT        NOT NULL IDENTITY(1,1),
        [UserId]              BIGINT        NOT NULL,
        [MethodType]          NVARCHAR(20)  NOT NULL, -- 'TOTP', 'Email', 'SMS'
        [SecretEncrypted]     NVARCHAR(512) NOT NULL, -- AES-encrypted TOTP secret
        [IsEnabled]           BIT           NOT NULL DEFAULT 0,
        [IsVerified]          BIT           NOT NULL DEFAULT 0,
        [CreatedAtUtc]        DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
        [EnabledAtUtc]        DATETIME2(7)  NULL,
        [DisabledAtUtc]       DATETIME2(7)  NULL,
        CONSTRAINT [PK_IdentityMfaMethods] PRIMARY KEY CLUSTERED ([MfaMethodId] ASC),
        CONSTRAINT [FK_MfaMethods_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[IdentityUsers]([UserId]),
        CONSTRAINT [UQ_MfaMethods_UserType] UNIQUE ([UserId],[MethodType])
    );
    CREATE INDEX [IX_MfaMethods_UserId] ON [dbo].[IdentityMfaMethods]([UserId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id=OBJECT_ID(N'[dbo].[IdentityRecoveryCodes]') AND type='U')
BEGIN
    CREATE TABLE [dbo].[IdentityRecoveryCodes] (
        [RecoveryCodeId]  BIGINT        NOT NULL IDENTITY(1,1),
        [UserId]          BIGINT        NOT NULL,
        [CodeHash]        NVARCHAR(128) NOT NULL, -- SHA-256 hash of the code
        [UsedAtUtc]       DATETIME2(7)  NULL,
        [IsUsed]          BIT           NOT NULL DEFAULT 0,
        [CreatedAtUtc]    DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_IdentityRecoveryCodes] PRIMARY KEY CLUSTERED ([RecoveryCodeId] ASC),
        CONSTRAINT [FK_RecoveryCodes_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[IdentityUsers]([UserId])
    );
    CREATE INDEX [IX_RecoveryCodes_UserId] ON [dbo].[IdentityRecoveryCodes]([UserId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id=OBJECT_ID(N'[dbo].[IdentityOtpChallenges]') AND type='U')
BEGIN
    CREATE TABLE [dbo].[IdentityOtpChallenges] (
        [ChallengeId]     BIGINT        NOT NULL IDENTITY(1,1),
        [UserId]          BIGINT        NOT NULL,
        [ChallengeToken]  NVARCHAR(128) NOT NULL, -- opaque token for the challenge session
        [OtpHash]         NVARCHAR(128) NULL,     -- hashed OTP (for email/SMS flows)
        [MethodType]      NVARCHAR(20)  NOT NULL,
        [IsUsed]          BIT           NOT NULL DEFAULT 0,
        [CreatedAtUtc]    DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
        [ExpiresAtUtc]    DATETIME2(7)  NOT NULL,
        CONSTRAINT [PK_IdentityOtpChallenges] PRIMARY KEY CLUSTERED ([ChallengeId] ASC),
        CONSTRAINT [FK_OtpChallenges_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[IdentityUsers]([UserId])
    );
    CREATE INDEX [IX_OtpChallenges_UserId] ON [dbo].[IdentityOtpChallenges]([UserId]);
    CREATE INDEX [IX_OtpChallenges_Token] ON [dbo].[IdentityOtpChallenges]([ChallengeToken]);
END
GO
