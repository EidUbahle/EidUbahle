-- =============================================================================
-- EidUbahle Enterprise ERP – Phase 3 Seed Data
-- Currencies, Default Chart of Accounts template, Accounting Permissions
-- Run after Schema_Phase3.sql.
-- =============================================================================

USE EidUbahleDB;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. Currencies
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM acc_Currencies WHERE Code='USD')
INSERT INTO acc_Currencies(Code,Name,Symbol,DecimalPlaces,IsBaseCurrency,IsActive)
VALUES
('USD','US Dollar','$',2,1,1),
('EUR','Euro','€',2,0,1),
('GBP','British Pound','£',2,0,1),
('SAR','Saudi Riyal','﷼',2,0,1),
('AED','UAE Dirham','د.إ',2,0,1),
('KES','Kenyan Shilling','KSh',2,0,1),
('SOS','Somali Shilling','Sh.So.',2,0,1),
('ETB','Ethiopian Birr','Br',2,0,1),
('TZS','Tanzanian Shilling','TSh',2,0,1),
('JPY','Japanese Yen','¥',0,0,1),
('CNY','Chinese Yuan','¥',2,0,1),
('CAD','Canadian Dollar','C$',2,0,1),
('AUD','Australian Dollar','A$',2,0,1),
('CHF','Swiss Franc','Fr',2,0,1),
('INR','Indian Rupee','₹',2,0,1);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. Accounting Permissions
-- ─────────────────────────────────────────────────────────────────────────────

-- Chart of Accounts
INSERT INTO sys_Permissions(Id,Module,Feature,Action,PermissionKey)
SELECT NEWID(),p.Module,p.Feature,p.Action,p.PermissionKey
FROM (VALUES
  ('Accounting','ChartOfAccounts','View',   'accounting.coa.view'),
  ('Accounting','ChartOfAccounts','Create',  'accounting.coa.create'),
  ('Accounting','ChartOfAccounts','Edit',    'accounting.coa.edit'),
  ('Accounting','ChartOfAccounts','Delete',  'accounting.coa.delete'),
  ('Accounting','JournalEntry',   'View',    'accounting.journal.view'),
  ('Accounting','JournalEntry',   'Create',  'accounting.journal.create'),
  ('Accounting','JournalEntry',   'Edit',    'accounting.journal.edit'),
  ('Accounting','JournalEntry',   'Delete',  'accounting.journal.delete'),
  ('Accounting','JournalEntry',   'Post',    'accounting.journal.post'),
  ('Accounting','JournalEntry',   'Reverse', 'accounting.journal.reverse'),
  ('Accounting','FiscalPeriod',   'View',    'accounting.period.view'),
  ('Accounting','FiscalPeriod',   'Create',  'accounting.period.create'),
  ('Accounting','FiscalPeriod',   'Edit',    'accounting.period.edit'),
  ('Accounting','FiscalPeriod',   'Close',   'accounting.period.close'),
  ('Accounting','Currency',       'View',    'accounting.currency.view'),
  ('Accounting','Currency',       'Create',  'accounting.currency.create'),
  ('Accounting','Currency',       'Edit',    'accounting.currency.edit'),
  ('Banking',   'BankAccount',    'View',    'banking.account.view'),
  ('Banking',   'BankAccount',    'Create',  'banking.account.create'),
  ('Banking',   'BankAccount',    'Edit',    'banking.account.edit'),
  ('Banking',   'BankAccount',    'Delete',  'banking.account.delete'),
  ('Banking',   'Reconciliation', 'View',    'banking.reconciliation.view'),
  ('Banking',   'Reconciliation', 'Create',  'banking.reconciliation.create'),
  ('Banking',   'Reconciliation', 'Approve', 'banking.reconciliation.approve'),
  ('Reports',   'TrialBalance',   'View',    'reports.trialbalance.view'),
  ('Reports',   'TrialBalance',   'Export',  'reports.trialbalance.export'),
  ('Reports',   'BalanceSheet',   'View',    'reports.balancesheet.view'),
  ('Reports',   'BalanceSheet',   'Export',  'reports.balancesheet.export'),
  ('Reports',   'IncomeStatement','View',    'reports.incomestatement.view'),
  ('Reports',   'IncomeStatement','Export',  'reports.incomestatement.export')
) AS p(Module,Feature,Action,PermissionKey)
WHERE NOT EXISTS (SELECT 1 FROM sys_Permissions WHERE PermissionKey=p.PermissionKey);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. Grant accounting permissions to existing system roles
--    (Super Admin and Tenant Admin roles get all accounting permissions)
-- ─────────────────────────────────────────────────────────────────────────────

-- Grant all accounting/banking/reports permissions to SuperAdmin & TenantAdmin roles
INSERT INTO sys_RolePermissions(Id, RoleId, PermissionId, IsGranted)
SELECT NEWID(), r.Id, p.Id, 1
FROM sys_Roles r
CROSS JOIN sys_Permissions p
WHERE r.Name IN ('Super Admin', 'Tenant Admin')
  AND p.Module IN ('Accounting', 'Banking', 'Reports')
  AND r.IsSystem = 1
  AND NOT EXISTS (
      SELECT 1 FROM sys_RolePermissions rp
      WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );
GO

-- Grant view/create/edit/post accounting permissions to Accountant role
INSERT INTO sys_RolePermissions(Id, RoleId, PermissionId, IsGranted)
SELECT NEWID(), r.Id, p.Id, 1
FROM sys_Roles r
CROSS JOIN sys_Permissions p
WHERE r.Name = 'Accountant'
  AND p.PermissionKey IN (
      'accounting.coa.view', 'accounting.coa.create', 'accounting.coa.edit',
      'accounting.journal.view', 'accounting.journal.create', 'accounting.journal.edit', 'accounting.journal.post',
      'accounting.period.view', 'accounting.currency.view',
      'banking.account.view', 'banking.reconciliation.view', 'banking.reconciliation.create',
      'reports.trialbalance.view', 'reports.balancesheet.view', 'reports.incomestatement.view'
  )
  AND r.IsSystem = 1
  AND NOT EXISTS (
      SELECT 1 FROM sys_RolePermissions rp
      WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );
GO

-- Grant view-only accounting permissions to Viewer/Auditor role
INSERT INTO sys_RolePermissions(Id, RoleId, PermissionId, IsGranted)
SELECT NEWID(), r.Id, p.Id, 1
FROM sys_Roles r
CROSS JOIN sys_Permissions p
WHERE r.Name IN ('Viewer', 'Auditor')
  AND p.PermissionKey IN (
      'accounting.coa.view', 'accounting.journal.view', 'accounting.period.view',
      'accounting.currency.view', 'banking.account.view', 'banking.reconciliation.view',
      'reports.trialbalance.view', 'reports.balancesheet.view', 'reports.incomestatement.view',
      'reports.trialbalance.export', 'reports.balancesheet.export', 'reports.incomestatement.export'
  )
  AND r.IsSystem = 1
  AND NOT EXISTS (
      SELECT 1 FROM sys_RolePermissions rp
      WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 4. Feature Flags for Phase 3 modules
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys_FeatureFlags WHERE TenantId IS NULL AND FeatureKey='accounting.enabled')
INSERT INTO sys_FeatureFlags(Id, TenantId, FeatureKey, IsEnabled, Notes)
VALUES
(NEWID(), NULL, 'accounting.enabled',     1, 'Core accounting module'),
(NEWID(), NULL, 'banking.enabled',        1, 'Bank accounts & reconciliation'),
(NEWID(), NULL, 'multicurrency.enabled',  1, 'Multi-currency support'),
(NEWID(), NULL, 'reports.enabled',        1, 'Financial reporting');
GO

PRINT 'Phase 3 seeds applied successfully.';
GO
