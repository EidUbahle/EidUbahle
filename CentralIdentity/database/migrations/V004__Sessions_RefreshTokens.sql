-- ============================================================
-- Script: V004__Sessions_RefreshTokens.sql
-- Description: Adds Phase 4 session management, refresh token,
--              and audit log tables. Idempotent: safe to re-run.
-- ============================================================

USE CentralIdentityDb;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[IdentitySessions]')
      AND type = 'U'
)
BEGIN
    CREATE TABLE [dbo].[IdentitySessions] (
        [SessionId]          UNIQUEIDENTIFIER NOT NULL,
        [UserId]             BIGINT           NOT NULL,
        [ApplicationId]      BIGINT           NOT NULL,
        [ClientId]           NVARCHAR(128)    NOT NULL,
        [CreatedAtUtc]       DATETIME2(7)     NOT NULL,
        [LastActivityAtUtc]  DATETIME2(7)     NOT NULL,
        [ExpiresAtUtc]       DATETIME2(7)     NOT NULL,
        [RevokedAtUtc]       DATETIME2(7)     NULL,
        [RevocationReason]   NVARCHAR(500)    NULL,
        [IpAddress]          NVARCHAR(100)    NULL,
        [UserAgent]          NVARCHAR(1024)   NULL,
        [DeviceId]           NVARCHAR(200)    NULL,
        [SecurityStamp]      NVARCHAR(128)    NOT NULL,
        [IsActive]           BIT              NOT NULL DEFAULT 1,
        CONSTRAINT [PK_IdentitySessions] PRIMARY KEY CLUSTERED ([SessionId] ASC),
        CONSTRAINT [FK_IdentitySessions_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[IdentityUsers]([UserId]),
        CONSTRAINT [FK_IdentitySessions_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[IdentityApplications]([ApplicationId])
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[IdentityRefreshTokens]')
      AND type = 'U'
)
BEGIN
    CREATE TABLE [dbo].[IdentityRefreshTokens] (
        [RefreshTokenId]     UNIQUEIDENTIFIER NOT NULL,
        [UserId]             BIGINT           NOT NULL,
        [ApplicationId]      BIGINT           NOT NULL,
        [SessionId]          UNIQUEIDENTIFIER NOT NULL,
        [TokenHash]          NVARCHAR(128)    NOT NULL,
        [CreatedAtUtc]       DATETIME2(7)     NOT NULL,
        [ExpiresAtUtc]       DATETIME2(7)     NOT NULL,
        [LastUsedAtUtc]      DATETIME2(7)     NULL,
        [RevokedAtUtc]       DATETIME2(7)     NULL,
        [ReplacedByTokenId]  UNIQUEIDENTIFIER NULL,
        [RevocationReason]   NVARCHAR(500)    NULL,
        [TokenFamilyId]      UNIQUEIDENTIFIER NOT NULL,
        [CreatedIpAddress]   NVARCHAR(100)    NULL,
        [LastUsedIpAddress]  NVARCHAR(100)    NULL,
        [UserAgent]          NVARCHAR(1024)   NULL,
        [Scope]              NVARCHAR(500)    NOT NULL DEFAULT '',
        CONSTRAINT [PK_IdentityRefreshTokens] PRIMARY KEY CLUSTERED ([RefreshTokenId] ASC),
        CONSTRAINT [UQ_IdentityRefreshTokens_TokenHash] UNIQUE ([TokenHash]),
        CONSTRAINT [FK_IdentityRefreshTokens_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[IdentityUsers]([UserId]),
        CONSTRAINT [FK_IdentityRefreshTokens_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[IdentityApplications]([ApplicationId]),
        CONSTRAINT [FK_IdentityRefreshTokens_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[IdentitySessions]([SessionId])
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[IdentityAuditLogs]')
      AND type = 'U'
)
BEGIN
    CREATE TABLE [dbo].[IdentityAuditLogs] (
        [AuditLogId]         BIGINT           NOT NULL IDENTITY(1,1),
        [UserId]             BIGINT           NULL,
        [ApplicationId]      BIGINT           NULL,
        [EventType]          NVARCHAR(100)    NOT NULL,
        [Severity]           NVARCHAR(30)     NOT NULL,
        [IpAddress]          NVARCHAR(100)    NULL,
        [UserAgent]          NVARCHAR(1024)   NULL,
        [Description]        NVARCHAR(2000)   NOT NULL,
        [CorrelationId]      NVARCHAR(100)    NULL,
        [CreatedAtUtc]       DATETIME2(7)     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_IdentityAuditLogs] PRIMARY KEY CLUSTERED ([AuditLogId] ASC),
        CONSTRAINT [FK_IdentityAuditLogs_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[IdentityUsers]([UserId]),
        CONSTRAINT [FK_IdentityAuditLogs_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[IdentityApplications]([ApplicationId])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IdentitySessions_UserId_IsActive' AND object_id = OBJECT_ID(N'[dbo].[IdentitySessions]'))
BEGIN
    CREATE INDEX [IX_IdentitySessions_UserId_IsActive] ON [dbo].[IdentitySessions]([UserId], [IsActive], [ExpiresAtUtc]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IdentitySessions_User_App_IsActive' AND object_id = OBJECT_ID(N'[dbo].[IdentitySessions]'))
BEGIN
    CREATE INDEX [IX_IdentitySessions_User_App_IsActive] ON [dbo].[IdentitySessions]([UserId], [ApplicationId], [IsActive], [ExpiresAtUtc]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IdentityRefreshTokens_SessionId' AND object_id = OBJECT_ID(N'[dbo].[IdentityRefreshTokens]'))
BEGIN
    CREATE INDEX [IX_IdentityRefreshTokens_SessionId] ON [dbo].[IdentityRefreshTokens]([SessionId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IdentityRefreshTokens_FamilyId' AND object_id = OBJECT_ID(N'[dbo].[IdentityRefreshTokens]'))
BEGIN
    CREATE INDEX [IX_IdentityRefreshTokens_FamilyId] ON [dbo].[IdentityRefreshTokens]([TokenFamilyId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IdentityRefreshTokens_User_App' AND object_id = OBJECT_ID(N'[dbo].[IdentityRefreshTokens]'))
BEGIN
    CREATE INDEX [IX_IdentityRefreshTokens_User_App] ON [dbo].[IdentityRefreshTokens]([UserId], [ApplicationId], [RevokedAtUtc], [ExpiresAtUtc]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IdentityAuditLogs_UserId_CreatedAtUtc' AND object_id = OBJECT_ID(N'[dbo].[IdentityAuditLogs]'))
BEGIN
    CREATE INDEX [IX_IdentityAuditLogs_UserId_CreatedAtUtc] ON [dbo].[IdentityAuditLogs]([UserId], [CreatedAtUtc]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IdentityAuditLogs_ApplicationId_CreatedAtUtc' AND object_id = OBJECT_ID(N'[dbo].[IdentityAuditLogs]'))
BEGIN
    CREATE INDEX [IX_IdentityAuditLogs_ApplicationId_CreatedAtUtc] ON [dbo].[IdentityAuditLogs]([ApplicationId], [CreatedAtUtc]);
END
GO
