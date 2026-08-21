-- ============================================================
-- Script: V005__Roles_Permissions.sql
-- Description: Creates RBAC tables: IdentityRoles, IdentityPermissions,
--              IdentityUserRoles, IdentityRolePermissions
-- Idempotent: safe to re-run.
-- ============================================================

USE CentralIdentityDb;
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id=OBJECT_ID(N'[dbo].[IdentityRoles]') AND type='U')
BEGIN
    CREATE TABLE [dbo].[IdentityRoles] (
        [RoleId]          BIGINT        NOT NULL IDENTITY(1,1),
        [ApplicationId]   BIGINT        NOT NULL,
        [RoleCode]        NVARCHAR(100) NOT NULL,
        [RoleName]        NVARCHAR(200) NOT NULL,
        [Description]     NVARCHAR(500) NULL,
        [IsActive]        BIT           NOT NULL DEFAULT 1,
        [CreatedAtUtc]    DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAtUtc]    DATETIME2(7)  NULL,
        CONSTRAINT [PK_IdentityRoles] PRIMARY KEY CLUSTERED ([RoleId] ASC),
        CONSTRAINT [UQ_IdentityRoles_AppCode] UNIQUE ([ApplicationId],[RoleCode]),
        CONSTRAINT [FK_IdentityRoles_Application] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[IdentityApplications]([ApplicationId])
    );
    CREATE INDEX [IX_IdentityRoles_ApplicationId] ON [dbo].[IdentityRoles]([ApplicationId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id=OBJECT_ID(N'[dbo].[IdentityPermissions]') AND type='U')
BEGIN
    CREATE TABLE [dbo].[IdentityPermissions] (
        [PermissionId]    BIGINT        NOT NULL IDENTITY(1,1),
        [ApplicationId]   BIGINT        NOT NULL,
        [PermissionCode]  NVARCHAR(100) NOT NULL,
        [PermissionName]  NVARCHAR(200) NOT NULL,
        [Description]     NVARCHAR(500) NULL,
        [IsActive]        BIT           NOT NULL DEFAULT 1,
        [CreatedAtUtc]    DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAtUtc]    DATETIME2(7)  NULL,
        CONSTRAINT [PK_IdentityPermissions] PRIMARY KEY CLUSTERED ([PermissionId] ASC),
        CONSTRAINT [UQ_IdentityPermissions_AppCode] UNIQUE ([ApplicationId],[PermissionCode]),
        CONSTRAINT [FK_IdentityPermissions_Application] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[IdentityApplications]([ApplicationId])
    );
    CREATE INDEX [IX_IdentityPermissions_ApplicationId] ON [dbo].[IdentityPermissions]([ApplicationId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id=OBJECT_ID(N'[dbo].[IdentityRolePermissions]') AND type='U')
BEGIN
    CREATE TABLE [dbo].[IdentityRolePermissions] (
        [RolePermissionId] BIGINT       NOT NULL IDENTITY(1,1),
        [RoleId]           BIGINT       NOT NULL,
        [PermissionId]     BIGINT       NOT NULL,
        [AssignedAtUtc]    DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_IdentityRolePermissions] PRIMARY KEY CLUSTERED ([RolePermissionId] ASC),
        CONSTRAINT [UQ_IdentityRolePermissions] UNIQUE ([RoleId],[PermissionId]),
        CONSTRAINT [FK_RolePermissions_Role] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[IdentityRoles]([RoleId]),
        CONSTRAINT [FK_RolePermissions_Permission] FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[IdentityPermissions]([PermissionId])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id=OBJECT_ID(N'[dbo].[IdentityUserRoles]') AND type='U')
BEGIN
    CREATE TABLE [dbo].[IdentityUserRoles] (
        [UserRoleId]      BIGINT        NOT NULL IDENTITY(1,1),
        [UserId]          BIGINT        NOT NULL,
        [ApplicationId]   BIGINT        NOT NULL,
        [RoleId]          BIGINT        NOT NULL,
        [AssignedAtUtc]   DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
        [RevokedAtUtc]    DATETIME2(7)  NULL,
        [IsActive]        BIT           NOT NULL DEFAULT 1,
        CONSTRAINT [PK_IdentityUserRoles] PRIMARY KEY CLUSTERED ([UserRoleId] ASC),
        CONSTRAINT [FK_UserRoles_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[IdentityUsers]([UserId]),
        CONSTRAINT [FK_UserRoles_Application] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[IdentityApplications]([ApplicationId]),
        CONSTRAINT [FK_UserRoles_Role] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[IdentityRoles]([RoleId])
    );
    CREATE INDEX [IX_IdentityUserRoles_UserApp] ON [dbo].[IdentityUserRoles]([UserId],[ApplicationId]);
    CREATE INDEX [IX_IdentityUserRoles_RoleId] ON [dbo].[IdentityUserRoles]([RoleId]);
END
GO
