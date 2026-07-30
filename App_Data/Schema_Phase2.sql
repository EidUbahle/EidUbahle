-- =============================================================================
-- EidUbahle Enterprise ERP – Phase 2 Database Schema
-- Multi-Tenancy, Users, Roles, RBAC, Audit Log, Invitations
-- Run this script against EidUbahleDB (after Schema_Phase1.sql).
-- =============================================================================

USE EidUbahleDB;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. Branches (Phase 1 created table, ensure it exists with all Phase 2 cols)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='saas_Branches')
CREATE TABLE saas_Branches (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    CompanyId       UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Companies(Id),
    Name            NVARCHAR(200)    NOT NULL,
    Code            NVARCHAR(20),
    Address         NVARCHAR(500),
    Phone           NVARCHAR(50),
    IsHeadOffice    BIT              NOT NULL DEFAULT 0,
    IsActive        BIT              NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted       BIT              NOT NULL DEFAULT 0,
    DeletedAt       DATETIME2,
    Version         ROWVERSION
);
GO

CREATE INDEX IX_Branches_TenantId ON saas_Branches(TenantId) WHERE IsDeleted=0;
CREATE INDEX IX_Branches_CompanyId ON saas_Branches(CompanyId) WHERE IsDeleted=0;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. Roles
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_Roles')
CREATE TABLE sys_Roles (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    Name            NVARCHAR(100)    NOT NULL,
    Description     NVARCHAR(500),
    IsSystem        BIT              NOT NULL DEFAULT 0,
    IsActive        BIT              NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted       BIT              NOT NULL DEFAULT 0
);
GO
CREATE INDEX IX_Roles_TenantId ON sys_Roles(TenantId) WHERE IsDeleted=0;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. Permissions (global, not tenant-specific)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_Permissions')
CREATE TABLE sys_Permissions (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    Module          NVARCHAR(100)    NOT NULL,
    Feature         NVARCHAR(100)    NOT NULL,
    Action          NVARCHAR(50)     NOT NULL,
    PermissionKey   NVARCHAR(200)    NOT NULL UNIQUE
);
GO
CREATE UNIQUE INDEX UX_Permissions_Key ON sys_Permissions(PermissionKey);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 4. Role Permissions
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_RolePermissions')
CREATE TABLE sys_RolePermissions (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    RoleId          UNIQUEIDENTIFIER NOT NULL REFERENCES sys_Roles(Id),
    PermissionId    UNIQUEIDENTIFIER NOT NULL REFERENCES sys_Permissions(Id),
    IsGranted       BIT              NOT NULL DEFAULT 1,
    CONSTRAINT UQ_RolePerm UNIQUE (RoleId, PermissionId)
);
GO
CREATE INDEX IX_RolePermissions_RoleId ON sys_RolePermissions(RoleId);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 5. User Roles
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_UserRoles')
CREATE TABLE sys_UserRoles (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    UserId          UNIQUEIDENTIFIER NOT NULL REFERENCES sys_Users(Id),
    RoleId          UNIQUEIDENTIFIER NOT NULL REFERENCES sys_Roles(Id),
    CompanyId       UNIQUEIDENTIFIER REFERENCES saas_Companies(Id),
    BranchId        UNIQUEIDENTIFIER REFERENCES saas_Branches(Id),
    AssignedAt      DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);
GO
CREATE INDEX IX_UserRoles_UserId ON sys_UserRoles(UserId);
CREATE INDEX IX_UserRoles_RoleId ON sys_UserRoles(RoleId);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 6. User Company-Branch assignments
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_UserCompanyBranches')
CREATE TABLE sys_UserCompanyBranches (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    UserId          UNIQUEIDENTIFIER NOT NULL REFERENCES sys_Users(Id),
    CompanyId       UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Companies(Id),
    BranchId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Branches(Id),
    CONSTRAINT UQ_UserBranch UNIQUE (UserId, BranchId)
);
GO
CREATE INDEX IX_UCB_UserId ON sys_UserCompanyBranches(UserId);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 7. Invitations
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_Invitations')
CREATE TABLE sys_Invitations (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    InvitedBy       UNIQUEIDENTIFIER NOT NULL REFERENCES sys_Users(Id),
    Email           NVARCHAR(200)    NOT NULL,
    FullName        NVARCHAR(200),
    Token           NVARCHAR(100)    NOT NULL,
    Status          NVARCHAR(20)     NOT NULL DEFAULT 'Pending', -- Pending|Accepted|Expired|Cancelled
    RoleIds         NVARCHAR(MAX),   -- JSON array of role GUIDs
    ExpiresAt       DATETIME2        NOT NULL,
    AcceptedAt      DATETIME2,
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);
GO
CREATE UNIQUE INDEX UX_Invitations_Token ON sys_Invitations(Token);
CREATE INDEX IX_Invitations_TenantId ON sys_Invitations(TenantId);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 8. Audit Log
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_AuditLog')
CREATE TABLE sys_AuditLog (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL,
    UserId          UNIQUEIDENTIFIER,
    Username        NVARCHAR(100),
    Action          NVARCHAR(50)     NOT NULL,  -- Create|Update|Delete|Login|Logout|Approve...
    EntityType      NVARCHAR(100),
    EntityId        UNIQUEIDENTIFIER,
    OldValues       NVARCHAR(MAX),   -- JSON
    NewValues       NVARCHAR(MAX),   -- JSON
    IpAddress       NVARCHAR(50),
    UserAgent       NVARCHAR(500),
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);
GO
CREATE INDEX IX_AuditLog_TenantId_CreatedAt ON sys_AuditLog(TenantId, CreatedAt DESC);
CREATE INDEX IX_AuditLog_UserId ON sys_AuditLog(UserId);
CREATE INDEX IX_AuditLog_EntityId ON sys_AuditLog(EntityId) WHERE EntityId IS NOT NULL;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 9. Feature Flags (per tenant)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='sys_FeatureFlags')
CREATE TABLE sys_FeatureFlags (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER REFERENCES saas_Tenants(Id),  -- NULL = global default
    FeatureKey      NVARCHAR(200)    NOT NULL,
    IsEnabled       BIT              NOT NULL DEFAULT 0,
    Notes           NVARCHAR(500),
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);
GO
CREATE INDEX IX_FeatureFlags_TenantId ON sys_FeatureFlags(TenantId);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 10. sys_Users – add missing columns (idempotent)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('sys_Users') AND name='LastSyncAt')
    ALTER TABLE sys_Users ADD LastSyncAt DATETIME2;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('sys_Users') AND name='DeletedAt')
    ALTER TABLE sys_Users ADD DeletedAt DATETIME2;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 11. Stored Procedures
-- ─────────────────────────────────────────────────────────────────────────────

-- sp_GetUserPermissions: returns all granted permission keys for a user
IF OBJECT_ID('dbo.sp_GetUserPermissions') IS NOT NULL DROP PROCEDURE dbo.sp_GetUserPermissions;
GO
CREATE PROCEDURE dbo.sp_GetUserPermissions
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT p.PermissionKey
    FROM sys_RolePermissions rp
    JOIN sys_Permissions p ON p.Id = rp.PermissionId
    JOIN sys_UserRoles ur ON ur.RoleId = rp.RoleId
    WHERE ur.UserId = @UserId AND rp.IsGranted = 1;
END;
GO

-- sp_WriteAuditLog: fire-and-forget audit entry
IF OBJECT_ID('dbo.sp_WriteAuditLog') IS NOT NULL DROP PROCEDURE dbo.sp_WriteAuditLog;
GO
CREATE PROCEDURE dbo.sp_WriteAuditLog
    @TenantId   UNIQUEIDENTIFIER,
    @UserId     UNIQUEIDENTIFIER = NULL,
    @Username   NVARCHAR(100)    = NULL,
    @Action     NVARCHAR(50),
    @EntityType NVARCHAR(100)    = NULL,
    @EntityId   UNIQUEIDENTIFIER = NULL,
    @OldValues  NVARCHAR(MAX)    = NULL,
    @NewValues  NVARCHAR(MAX)    = NULL,
    @IpAddress  NVARCHAR(50)     = NULL,
    @UserAgent  NVARCHAR(500)    = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO sys_AuditLog(Id,TenantId,UserId,Username,Action,EntityType,EntityId,
                              OldValues,NewValues,IpAddress,UserAgent,CreatedAt)
    VALUES(NEWID(),@TenantId,@UserId,@Username,@Action,@EntityType,@EntityId,
           @OldValues,@NewValues,@IpAddress,@UserAgent,GETUTCDATE());
END;
GO

PRINT 'Phase 2 schema applied successfully.';
GO
