-- =============================================================================
-- EidUbahle ERP – Phase 2 Seed Data
-- System Roles + Full Permission Matrix
-- Requires: Schema_Phase1.sql and Schema_Phase2.sql already executed.
-- Run once per database.
-- =============================================================================

USE EidUbahleDB;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. Permissions – full RBAC matrix
--    Format: Module | Feature | Action | PermissionKey
-- ─────────────────────────────────────────────────────────────────────────────

MERGE sys_Permissions AS target
USING (VALUES
    -- ── Accounting ──────────────────────────────────────────────────────
    ('Accounting','Chart of Accounts','View',   'accounting.coa.view'),
    ('Accounting','Chart of Accounts','Create', 'accounting.coa.create'),
    ('Accounting','Chart of Accounts','Edit',   'accounting.coa.edit'),
    ('Accounting','Chart of Accounts','Delete', 'accounting.coa.delete'),
    ('Accounting','Journal Entry','View',        'accounting.journal.view'),
    ('Accounting','Journal Entry','Create',      'accounting.journal.create'),
    ('Accounting','Journal Entry','Edit',        'accounting.journal.edit'),
    ('Accounting','Journal Entry','Delete',      'accounting.journal.delete'),
    ('Accounting','Journal Entry','Approve',     'accounting.journal.approve'),
    ('Accounting','Journal Entry','Post',        'accounting.journal.post'),
    ('Accounting','Journal Entry','Reverse',     'accounting.journal.reverse'),
    ('Accounting','Fiscal Period','View',         'accounting.period.view'),
    ('Accounting','Fiscal Period','Manage',       'accounting.period.manage'),
    -- ── Banking ──────────────────────────────────────────────────────────
    ('Banking','Bank Accounts','View',           'banking.account.view'),
    ('Banking','Bank Accounts','Create',         'banking.account.create'),
    ('Banking','Bank Accounts','Edit',           'banking.account.edit'),
    ('Banking','Bank Accounts','Delete',         'banking.account.delete'),
    ('Banking','Reconciliation','View',          'banking.reconciliation.view'),
    ('Banking','Reconciliation','Process',       'banking.reconciliation.process'),
    ('Banking','Transfers','View',               'banking.transfer.view'),
    ('Banking','Transfers','Create',             'banking.transfer.create'),
    -- ── Sales ─────────────────────────────────────────────────────────────
    ('Sales','Customers','View',                 'sales.customer.view'),
    ('Sales','Customers','Create',               'sales.customer.create'),
    ('Sales','Customers','Edit',                 'sales.customer.edit'),
    ('Sales','Customers','Delete',               'sales.customer.delete'),
    ('Sales','Invoices','View',                  'sales.invoice.view'),
    ('Sales','Invoices','Create',                'sales.invoice.create'),
    ('Sales','Invoices','Edit',                  'sales.invoice.edit'),
    ('Sales','Invoices','Delete',                'sales.invoice.delete'),
    ('Sales','Invoices','Approve',               'sales.invoice.approve'),
    ('Sales','Invoices','Export',                'sales.invoice.export'),
    ('Sales','Receipts','View',                  'sales.receipt.view'),
    ('Sales','Receipts','Create',                'sales.receipt.create'),
    -- ── Purchases ────────────────────────────────────────────────────────
    ('Purchases','Suppliers','View',             'purchases.supplier.view'),
    ('Purchases','Suppliers','Create',           'purchases.supplier.create'),
    ('Purchases','Suppliers','Edit',             'purchases.supplier.edit'),
    ('Purchases','Suppliers','Delete',           'purchases.supplier.delete'),
    ('Purchases','Purchase Orders','View',       'purchases.po.view'),
    ('Purchases','Purchase Orders','Create',     'purchases.po.create'),
    ('Purchases','Purchase Orders','Edit',       'purchases.po.edit'),
    ('Purchases','Purchase Orders','Delete',     'purchases.po.delete'),
    ('Purchases','Purchase Orders','Approve',    'purchases.po.approve'),
    ('Purchases','Bills','View',                 'purchases.bill.view'),
    ('Purchases','Bills','Create',               'purchases.bill.create'),
    ('Purchases','Bills','Edit',                 'purchases.bill.edit'),
    ('Purchases','Bills','Approve',              'purchases.bill.approve'),
    -- ── Inventory ─────────────────────────────────────────────────────────
    ('Inventory','Products','View',              'inventory.product.view'),
    ('Inventory','Products','Create',            'inventory.product.create'),
    ('Inventory','Products','Edit',              'inventory.product.edit'),
    ('Inventory','Products','Delete',            'inventory.product.delete'),
    ('Inventory','Categories','View',            'inventory.category.view'),
    ('Inventory','Categories','Manage',          'inventory.category.manage'),
    ('Inventory','Stock Movements','View',       'inventory.movement.view'),
    ('Inventory','Stock Movements','Create',     'inventory.movement.create'),
    ('Inventory','Stock Adjustments','Approve',  'inventory.adjustment.approve'),
    -- ── Payroll ───────────────────────────────────────────────────────────
    ('Payroll','Employees','View',               'payroll.employee.view'),
    ('Payroll','Employees','Create',             'payroll.employee.create'),
    ('Payroll','Employees','Edit',               'payroll.employee.edit'),
    ('Payroll','Employees','Delete',             'payroll.employee.delete'),
    ('Payroll','Pay Runs','View',                'payroll.payrun.view'),
    ('Payroll','Pay Runs','Create',              'payroll.payrun.create'),
    ('Payroll','Pay Runs','Approve',             'payroll.payrun.approve'),
    ('Payroll','Pay Runs','Export',              'payroll.payrun.export'),
    -- ── Reports ───────────────────────────────────────────────────────────
    ('Reports','Financial Statements','View',    'reports.financial.view'),
    ('Reports','Financial Statements','Export',  'reports.financial.export'),
    ('Reports','Sales Reports','View',           'reports.sales.view'),
    ('Reports','Sales Reports','Export',         'reports.sales.export'),
    ('Reports','Inventory Reports','View',       'reports.inventory.view'),
    ('Reports','Payroll Reports','View',         'reports.payroll.view'),
    ('Reports','Audit Trail','View',             'reports.audit.view'),
    ('Reports','Audit Trail','Export',           'reports.audit.export'),
    -- ── Admin ─────────────────────────────────────────────────────────────
    ('Admin','Users','View',                     'admin.user.view'),
    ('Admin','Users','Create',                   'admin.user.create'),
    ('Admin','Users','Edit',                     'admin.user.edit'),
    ('Admin','Users','Delete',                   'admin.user.delete'),
    ('Admin','Roles','View',                     'admin.role.view'),
    ('Admin','Roles','Manage',                   'admin.role.manage'),
    ('Admin','Companies','View',                 'admin.company.view'),
    ('Admin','Companies','Manage',               'admin.company.manage'),
    ('Admin','Tenant Settings','View',           'admin.tenant.view'),
    ('Admin','Tenant Settings','Edit',           'admin.tenant.edit'),
    ('Admin','Translations','View',              'admin.translation.view'),
    ('Admin','Translations','Manage',            'admin.translation.manage')
) AS source (Module, Feature, Action, PermissionKey)
ON target.PermissionKey = source.PermissionKey
WHEN NOT MATCHED THEN
    INSERT (Id, Module, Feature, Action, PermissionKey)
    VALUES (NEWID(), source.Module, source.Feature, source.Action, source.PermissionKey);
GO

PRINT 'Permissions seeded.';
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. System Roles per Tenant
--    We create system roles for each existing tenant.
--    New tenants get system roles created at onboarding time.
-- ─────────────────────────────────────────────────────────────────────────────

DECLARE @TenantId UNIQUEIDENTIFIER;
DECLARE tenant_cursor CURSOR FOR
    SELECT Id FROM saas_Tenants WHERE IsDeleted = 0;

OPEN tenant_cursor;
FETCH NEXT FROM tenant_cursor INTO @TenantId;
WHILE @@FETCH_STATUS = 0
BEGIN

    -- ── Accountant ────────────────────────────────────────────────────────
    IF NOT EXISTS (SELECT 1 FROM sys_Roles WHERE TenantId=@TenantId AND Name='Accountant' AND IsSystem=1 AND IsDeleted=0)
    BEGIN
        DECLARE @AccId UNIQUEIDENTIFIER = NEWID();
        INSERT INTO sys_Roles(Id,TenantId,Name,Description,IsSystem,IsActive,IsDeleted,CreatedAt,UpdatedAt)
        VALUES(@AccId,@TenantId,'Accountant','Full access to accounting, banking, and financial reports',1,1,0,GETUTCDATE(),GETUTCDATE());
        INSERT INTO sys_RolePermissions(Id,RoleId,PermissionId,IsGranted)
        SELECT NEWID(),@AccId,Id,1 FROM sys_Permissions
        WHERE Module IN ('Accounting','Banking','Reports');
    END;

    -- ── Sales Representative ──────────────────────────────────────────────
    IF NOT EXISTS (SELECT 1 FROM sys_Roles WHERE TenantId=@TenantId AND Name='Sales Representative' AND IsSystem=1 AND IsDeleted=0)
    BEGIN
        DECLARE @SalesId UNIQUEIDENTIFIER = NEWID();
        INSERT INTO sys_Roles(Id,TenantId,Name,Description,IsSystem,IsActive,IsDeleted,CreatedAt,UpdatedAt)
        VALUES(@SalesId,@TenantId,'Sales Representative','Create and manage sales invoices, customers and receipts',1,1,0,GETUTCDATE(),GETUTCDATE());
        INSERT INTO sys_RolePermissions(Id,RoleId,PermissionId,IsGranted)
        SELECT NEWID(),@SalesId,Id,1 FROM sys_Permissions
        WHERE Module = 'Sales';
    END;

    -- ── Purchasing Officer ─────────────────────────────────────────────────
    IF NOT EXISTS (SELECT 1 FROM sys_Roles WHERE TenantId=@TenantId AND Name='Purchasing Officer' AND IsSystem=1 AND IsDeleted=0)
    BEGIN
        DECLARE @PurId UNIQUEIDENTIFIER = NEWID();
        INSERT INTO sys_Roles(Id,TenantId,Name,Description,IsSystem,IsActive,IsDeleted,CreatedAt,UpdatedAt)
        VALUES(@PurId,@TenantId,'Purchasing Officer','Manage purchase orders, suppliers and bills',1,1,0,GETUTCDATE(),GETUTCDATE());
        INSERT INTO sys_RolePermissions(Id,RoleId,PermissionId,IsGranted)
        SELECT NEWID(),@PurId,Id,1 FROM sys_Permissions
        WHERE Module = 'Purchases';
    END;

    -- ── Inventory Manager ─────────────────────────────────────────────────
    IF NOT EXISTS (SELECT 1 FROM sys_Roles WHERE TenantId=@TenantId AND Name='Inventory Manager' AND IsSystem=1 AND IsDeleted=0)
    BEGIN
        DECLARE @InvId UNIQUEIDENTIFIER = NEWID();
        INSERT INTO sys_Roles(Id,TenantId,Name,Description,IsSystem,IsActive,IsDeleted,CreatedAt,UpdatedAt)
        VALUES(@InvId,@TenantId,'Inventory Manager','Full access to products, stock movements and categories',1,1,0,GETUTCDATE(),GETUTCDATE());
        INSERT INTO sys_RolePermissions(Id,RoleId,PermissionId,IsGranted)
        SELECT NEWID(),@InvId,Id,1 FROM sys_Permissions
        WHERE Module = 'Inventory';
    END;

    -- ── HR Manager ────────────────────────────────────────────────────────
    IF NOT EXISTS (SELECT 1 FROM sys_Roles WHERE TenantId=@TenantId AND Name='HR Manager' AND IsSystem=1 AND IsDeleted=0)
    BEGIN
        DECLARE @HRId UNIQUEIDENTIFIER = NEWID();
        INSERT INTO sys_Roles(Id,TenantId,Name,Description,IsSystem,IsActive,IsDeleted,CreatedAt,UpdatedAt)
        VALUES(@HRId,@TenantId,'HR Manager','Manage employees and payroll',1,1,0,GETUTCDATE(),GETUTCDATE());
        INSERT INTO sys_RolePermissions(Id,RoleId,PermissionId,IsGranted)
        SELECT NEWID(),@HRId,Id,1 FROM sys_Permissions
        WHERE Module = 'Payroll';
    END;

    -- ── Read Only ─────────────────────────────────────────────────────────
    IF NOT EXISTS (SELECT 1 FROM sys_Roles WHERE TenantId=@TenantId AND Name='Read Only' AND IsSystem=1 AND IsDeleted=0)
    BEGIN
        DECLARE @ROId UNIQUEIDENTIFIER = NEWID();
        INSERT INTO sys_Roles(Id,TenantId,Name,Description,IsSystem,IsActive,IsDeleted,CreatedAt,UpdatedAt)
        VALUES(@ROId,@TenantId,'Read Only','View-only access to all modules',1,1,0,GETUTCDATE(),GETUTCDATE());
        INSERT INTO sys_RolePermissions(Id,RoleId,PermissionId,IsGranted)
        SELECT NEWID(),@ROId,Id,1 FROM sys_Permissions
        WHERE Action IN ('View','Export');
    END;

    FETCH NEXT FROM tenant_cursor INTO @TenantId;
END;

CLOSE tenant_cursor;
DEALLOCATE tenant_cursor;
GO

PRINT 'System roles seeded for all existing tenants.';
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. Default Feature Flags (global)
-- ─────────────────────────────────────────────────────────────────────────────

INSERT INTO sys_FeatureFlags(Id, TenantId, FeatureKey, IsEnabled, Notes, CreatedAt, UpdatedAt)
SELECT NEWID(), NULL, fk, enabled, notes, GETUTCDATE(), GETUTCDATE()
FROM (VALUES
    ('accounting.enabled',  1, 'Core accounting module'),
    ('sales.enabled',       1, 'Sales and invoicing module'),
    ('purchases.enabled',   1, 'Purchasing module'),
    ('inventory.enabled',   1, 'Inventory management module'),
    ('payroll.enabled',     0, 'Payroll module (requires upgrade)'),
    ('crm.enabled',         0, 'CRM module (coming soon)'),
    ('projects.enabled',    0, 'Project management (coming soon)'),
    ('ecommerce.enabled',   0, 'E-commerce integration (coming soon)')
) AS x(fk, enabled, notes)
WHERE NOT EXISTS (SELECT 1 FROM sys_FeatureFlags WHERE TenantId IS NULL AND FeatureKey = x.fk);
GO

PRINT 'Feature flags seeded.';
GO

PRINT 'Phase 2 seeds applied successfully.';
GO
