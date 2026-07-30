# EidUbahle Enterprise ERP

**Phase 4 Complete – Inventory & Products**

A modern, production-ready **Enterprise Accounting SaaS Platform** built on:

| Layer | Technology |
|-------|-----------|
| Frontend | HTML5, CSS3, Bootstrap 5, JavaScript ES6, PWA, Service Workers |
| Backend | ASP.NET WebForms 4.8 (Master Pages, AJAX Handlers, no ScriptManager) |
| Offline | Dexie.js (IndexedDB), Background Sync |
| Database | MSSQL Server 2019+ |
| Auth | JWT (HS256) + Refresh Tokens + TOTP 2FA |

---

## Phase 1 Deliverables

- [x] Enterprise folder structure (SOLID, layered architecture)
- [x] Domain entities, enums, DTOs
- [x] MSSQL schema (`sys_*`, `saas_*`) with stored procedures
- [x] JWT authentication (HS256, no external libs) + Refresh tokens
- [x] TOTP 2FA
- [x] PBKDF2-SHA256 password hashing (100,000 iterations)
- [x] Multi-language engine (server + client, 8 languages, RTL/LTR)
- [x] Translation admin UI (inline edit, add language, export/import JSON)
- [x] Dexie.js offline schema (IndexedDB mirror of cloud schema)
- [x] Sync engine (push/pull, exponential backoff, conflict detection)
- [x] Service Worker (cache-first static, network-first API, push notifications)
- [x] PWA manifest (installable, shortcuts)
- [x] Theme engine (dark/light/auto, custom accent, CSS custom properties, no FOUC)
- [x] Master Page 1: Classic Sidebar (collapsible, responsive)
- [x] Master Page 2: Top Navigation
- [x] Master Page 3: Compact Header with collapsible icon sidebar
- [x] Login page (JWT, offline mode, 2FA, language switcher, PWA install prompt)
- [x] Dashboard skeleton (KPI cards, charts placeholder, recent transactions, quick actions)
- [x] JWT Auth HttpModule (validates every request, 401 on AJAX, redirect on pages)
- [x] AJAX handlers: Auth, Translations, SyncPush, SyncPull, Dashboard, UserPreference
- [x] Design system (CSS components: cards, buttons, tables, modals, badges, toasts, skeleton loaders)
- [x] Seed data: 8 languages, subscription plans, core permissions
- [x] Translation seeds (EN, SO, AR, FR for Auth, Navigation, Common, Admin, Sync)

---

## Phase 2 Deliverables

- [x] Phase 2 DTOs: User, Role, Permission, Company, Branch, Invite, Onboarding, AuditLog
- [x] **Repositories layer**: `UserRepository`, `RoleRepository`, `TenantRepository`
- [x] **Services**: `UserService` (create/invite/password/delete), `RoleService` (RBAC), `TenantService` (settings/company/branch/onboarding)
- [x] **ServiceLocator** updated with all Phase 2 services and repositories
- [x] **AJAX Handlers**: `Users.ashx` (full CRUD + invite + password), `Roles.ashx` (roles + permission matrix), `Tenant.ashx` (settings + company + branch + onboarding)
- [x] **User Management UI** (`Admin/Users.aspx`): list, search, filter, create, edit, invite, reset password, unlock, delete
- [x] **RBAC Permission Matrix** (`Admin/Roles.aspx`): visual checkbox grid, role CRUD, system role protection
- [x] **Companies & Branches** (`Admin/Companies.aspx`): accordion per company, full CRUD, head-office enforcement
- [x] **Tenant Settings** (`Admin/TenantSettings.aspx`): branding (logo, accent, theme, layout), locale, live accent preview, subscription/usage panel
- [x] **Tenant Onboarding Wizard** (`Pages/Onboarding.aspx`): 3-step wizard (company → branch → branding), standalone page (no master)
- [x] **Schema_Phase2.sql**: Branches, Roles, Permissions, RolePermissions, UserRoles, UserCompanyBranches, Invitations, AuditLog, FeatureFlags, stored procedures
- [x] **Seeds_Phase2.sql**: 80+ permissions across 7 modules, 6 system roles per tenant, feature flags
- [x] Sidebar navigation updated with all Admin links
- [x] Tenant limit enforcement (MaxUsers, MaxCompanies) in business logic
- [x] JwtAuthModule updated with authenticated page registry

---

## Setup Instructions

### 1. Prerequisites

- SQL Server 2019 or Azure SQL
- IIS or IIS Express with .NET 4.8
- Visual Studio 2019+ or VS Code

### 2. Database Setup

```sql
-- 1. Create the database
CREATE DATABASE EidUbahleDB;

-- 2. Run Phase 1 schema
-- Execute: App_Data/Schema_Phase1.sql

-- 3. Run Phase 2 schema
-- Execute: App_Data/Schema_Phase2.sql

-- 4. Run Phase 3 schema (Accounting)
-- Execute: App_Data/Schema_Phase3.sql

-- 5. Run Phase 4 schema (Inventory)
-- Execute: App_Data/Schema_Phase4.sql

-- 6. Seed translations
-- Execute: App_Data/Seeds_Translations.sql

-- 7. Seed Phase 2 (roles, permissions, feature flags)
-- Execute: App_Data/Seeds_Phase2.sql

-- 8. Seed Phase 3 (currencies, accounting permissions)
-- Execute: App_Data/Seeds_Phase3.sql

-- 9. Seed Phase 4 (inventory permissions)
-- Execute: App_Data/Seeds_Phase4.sql
```

### 3. Configuration

Edit `Web.config`:

```xml
<!-- Connection string -->
<add name="DefaultConnection"
     connectionString="Server=YOUR_SERVER;Database=EidUbahleDB;User Id=YOUR_USER;******;..."
     providerName="System.Data.SqlClient" />

<!-- IMPORTANT: Change this secret in production! -->
<add key="Jwt:SecretKey" value="YOUR-64-CHARACTER-RANDOM-SECRET-HERE" />

<!-- Environment -->
<add key="App:Environment" value="Production" />
```

### 4. Create First Super Admin

Run the schema scripts, then insert a tenant, company, branch, and user with your desired credentials. Use `PasswordService.HashPassword(password)` to generate the password hash.

### 5. Run the Application

```
iisexpress /path:C:\path\to\EidUbahle /port:5000
```

Navigate to `http://localhost:5000`

After first login, visit `/Pages/Onboarding.aspx` to complete the setup wizard.

---

## Architecture Overview

```
EidUbahle/
├── App_Code/
│   ├── Domain/
│   │   ├── Entities/       ← BaseEntity, CoreEntities (Tenant, User, Role, Translation, Sync...)
│   │   ├── Enums/          ← All system enumerations
│   │   └── DTOs/
│   │       ├── CoreDTOs.cs          ← Auth, Sync, API response DTOs
│   │       └── Phase2DTOs.cs        ← User, Role, Company, Branch, Invite, Onboarding DTOs
│   ├── Repositories/       ← NEW Phase 2
│   │   ├── UserRepository.cs        ← User CRUD, invitations
│   │   ├── RoleRepository.cs        ← Role/Permission CRUD
│   │   └── TenantRepository.cs      ← Tenant settings, company/branch CRUD
│   ├── Services/
│   │   ├── AuthService.cs           ← Login, refresh, logout
│   │   ├── UserService.cs           ← NEW: User management, invite, password
│   │   ├── RoleService.cs           ← NEW: RBAC role management
│   │   └── TenantService.cs         ← NEW: Tenant/company/branch/onboarding
│   ├── Infrastructure/
│   │   ├── Security/       ← JwtService, PasswordService, ConfigHelper
│   │   ├── Localization/   ← TranslationService
│   │   └── Caching/        ← AppMemoryCache
│   └── CrossCutting/
│       └── ServiceLocator  ← DI container (updated with Phase 2 services)
├── MasterPages/
│   ├── Classic.master      ← Updated: Admin links in sidebar
│   ├── TopNav.master
│   └── Compact.master
├── Pages/
│   ├── Login.aspx
│   ├── Dashboard.aspx
│   ├── Onboarding.aspx     ← NEW: 3-step setup wizard
│   └── Error.aspx
├── Admin/
│   ├── Users.aspx          ← NEW: User management UI
│   ├── Roles.aspx          ← NEW: RBAC permission matrix
│   ├── Companies.aspx      ← NEW: Companies & branches
│   ├── TenantSettings.aspx ← NEW: Branding & preferences
│   └── Translations.aspx
├── Handlers/
│   ├── Auth.ashx
│   ├── Users.ashx          ← NEW: User CRUD + invite + password
│   ├── Roles.ashx          ← NEW: Roles + permission matrix
│   ├── Tenant.ashx         ← NEW: Settings + company + branch + onboarding
│   ├── Translations.ashx
│   ├── SyncPush.ashx
│   ├── SyncPull.ashx
│   ├── Dashboard.ashx
│   └── UserPreference.ashx
├── Security/
│   └── JwtAuthModule.cs    ← Updated: authenticated page registry
├── App_Data/
│   ├── Schema_Phase1.sql
│   ├── Schema_Phase2.sql   ← NEW: Roles, Permissions, Invitations, AuditLog, FeatureFlags
│   ├── Seeds_Translations.sql
│   └── Seeds_Phase2.sql    ← NEW: 80+ permissions, 6 system roles, feature flags
└── ...
```

---

## Key Design Decisions

1. **Offline-First**: Every read/write hits IndexedDB first; sync queue handles cloud propagation.
2. **JWT without libraries**: Pure HS256 in `JwtService.cs` – no external dependencies.
3. **No ScriptManager / UpdatePanels**: All async is XHR + JSON via `.ashx` handlers.
4. **Zero hardcoded strings**: All UI text goes through `i18n.t('key')`.
5. **Decoupled BLL**: All services are pure C# classes – no WebForms dependency.
6. **RBAC at handler level**: Every handler reads `JwtClaims` from `HttpContext.Items`; admin actions check `adm`/`sad` claims.
7. **Tenant limit enforcement**: `UserService` and `TenantService` check `MaxUsers`/`MaxCompanies` before creation.
8. **System roles protected**: `IsSystem=true` roles cannot be edited or deleted by any user.

---

## Phase 3 Deliverables

- [x] **Schema_Phase3.sql**: Currencies, ExchangeRates, FiscalYears, FiscalPeriods, ChartOfAccounts, JournalEntries, JournalEntryLines, BankAccounts, BankReconciliations, AccountBalances (materialized cache), stored procedures (sp_PostJournalEntry, sp_GenerateJournalEntryNumber, sp_GetTrialBalance)
- [x] **Seeds_Phase3.sql**: 15 currencies (USD, EUR, GBP, SAR, AED, KES, SOS, ETB, TZS …), 30 accounting/banking/reports permissions, role assignments
- [x] **Phase3DTOs.cs**: Currencies, ExchangeRates, FiscalYear/Period, Account (COA), JournalEntry/Lines, TrialBalance, BalanceSheet, IncomeStatement, BankAccount, BankReconciliation DTOs
- [x] **AccountingRepository.cs**: Full data access layer for all Phase 3 entities
- [x] **AccountingService.cs**: Business logic – COA CRUD, Journal Entry with double-entry validation, Post/Reverse, Trial Balance, Balance Sheet, Income Statement, Bank Account CRUD, Reconciliation, Exchange Rates
- [x] **Handlers**: `Accounts.ashx`, `Journals.ashx`, `Reports.ashx`, `FiscalPeriods.ashx`, `Banks.ashx`
- [x] **Chart of Accounts** (`Pages/Accounting/ChartOfAccounts.aspx`): hierarchical tree view, CRUD, type/sub-type, posting/header flag, currency
- [x] **Journal Entry** (`Pages/Accounting/JournalEntry.aspx`): list + new entry form, double-entry line editor, live balance check, save draft/post, post, reverse, delete
- [x] **Fiscal Periods** (`Pages/Accounting/FiscalPeriods.aspx`): fiscal year CRUD with auto-generated 12 periods, open/close individual periods or entire year
- [x] **Financial Reports** (`Pages/Accounting/Reports.aspx`): Trial Balance, Balance Sheet, Income Statement with company/year/period filters
- [x] **Bank Accounts** (`Pages/Banking/BankAccounts.aspx`): CRUD, GL account linking, currency, opening balance
- [x] **Reports redirect** (`Pages/Reports/Index.aspx`)
- [x] **ServiceLocator** updated with AccountingService and AccountingRepository
- [x] **JwtAuthModule** updated with all new page paths
- [x] **Classic.master** sidebar updated with Fiscal Periods link

---

## Phase 4 Deliverables

- [x] **Schema_Phase4.sql**: `inv_Categories`, `inv_Brands`, `inv_UOM`, `inv_Products`, `inv_ProductVariants`, `inv_Warehouses`, `inv_WarehouseLocations`, `inv_StockLevels` (materialized, with computed `QtyAvailable`), `inv_CostLayers` (FIFO layers), `inv_StockMovements`, `inv_StockMovementLines`, `inv_Batches`, `inv_SerialNumbers`, `inv_StockReservations`, `inv_LowStockAlerts`. Stored procedures: `sp_GenerateMovementNumber`, `sp_PostStockMovement` (FIFO layer consumption + AVCO recalculation + low-stock alert generation), `sp_ReserveStock`, `sp_ReleaseReservation`, `sp_GetStockValuation`
- [x] **Seeds_Phase4.sql**: 38 inventory permissions (product/category/brand/uom/warehouse/movement/batch/reservation/valuation/alert)
- [x] **Phase4DTOs.cs**: Category, Brand, UOM, Product (list & detail), Variant, Warehouse, Location, StockLevel, StockMovement/Lines, Batch, SerialNumber, StockReservation, LowStockAlert, StockValuation/Summary DTOs
- [x] **InventoryRepository.cs**: Full data access layer – CRUD for all entities, `SeedDefaultUoms()` (12 standard units across Count/Weight/Volume/Length), batch/serial lookup, valuation via stored procedure
- [x] **InventoryService.cs**: Business logic – validation, category/brand/UOM management, product CRUD with stock-guard on delete, warehouse management, stock movement lifecycle (Draft → Post), FIFO/AVCO valuation report, low-stock alert management, reservation engine (reserve/release)
- [x] **Handlers**: `Products.ashx` (products + categories + brands + UOM), `Warehouses.ashx` (warehouses + locations + stock levels), `StockMovements.ashx` (movements + batches + serials), `Inventory.ashx` (stock levels + valuation + alerts + reservations)
- [x] **Products** (`Pages/Inventory/Products.aspx`): searchable/filterable product list, full CRUD modal, per-product stock level drill-down, FIFO/AVCO stock valuation modal, low-stock alert bell with badge
- [x] **Categories & Brands** (`Pages/Inventory/Categories.aspx`): hierarchical category tree CRUD, brand CRUD
- [x] **Warehouses** (`Pages/Inventory/Warehouses.aspx`): warehouse card grid, location management, company/branch assignment, default warehouse enforcement
- [x] **Stock Movements** (`Pages/Inventory/StockMovements.aspx`): Opening Stock / Adjustment / Transfer / Receipt / Issue – create with line editor, draft/post workflow, movement detail view
- [x] **ServiceLocator** updated with `InventoryService` and `InventoryRepository`
- [x] **JwtAuthModule** updated with all inventory page paths
- [x] **Classic.master** sidebar updated with Products, Categories & Brands, Warehouses, Stock Movements sub-links

---

## Next Phase

**Phase 5: Sales & Purchases**
- Customer management (CRM contacts)
- Sales invoices (with inventory issue + GL posting)
- Purchase orders and supplier invoices (with inventory receipt + GL posting)
- Payments & receipts
- Accounts Receivable / Payable aging

---



| Layer | Technology |
|-------|-----------|
| Frontend | HTML5, CSS3, Bootstrap 5, JavaScript ES6, PWA, Service Workers |
| Backend | ASP.NET WebForms 4.8 (Master Pages, AJAX Handlers, no ScriptManager) |
| Offline | Dexie.js (IndexedDB), Background Sync |
| Database | MSSQL Server 2019+ |
| Auth | JWT (HS256) + Refresh Tokens + TOTP 2FA |

---

## Phase 1 Deliverables

- [x] Enterprise folder structure (SOLID, layered architecture)
- [x] Domain entities, enums, DTOs
- [x] MSSQL schema (`sys_*`, `saas_*`) with stored procedures
- [x] JWT authentication (HS256, no external libs) + Refresh tokens
- [x] TOTP 2FA
- [x] PBKDF2-SHA256 password hashing (100,000 iterations)
- [x] Multi-language engine (server + client, 8 languages, RTL/LTR)
- [x] Translation admin UI (inline edit, add language, export/import JSON)
- [x] Dexie.js offline schema (IndexedDB mirror of cloud schema)
- [x] Sync engine (push/pull, exponential backoff, conflict detection)
- [x] Service Worker (cache-first static, network-first API, push notifications)
- [x] PWA manifest (installable, shortcuts)
- [x] Theme engine (dark/light/auto, custom accent, CSS custom properties, no FOUC)
- [x] Master Page 1: Classic Sidebar (collapsible, responsive)
- [x] Master Page 2: Top Navigation
- [x] Master Page 3: Compact Header with collapsible icon sidebar
- [x] Login page (JWT, offline mode, 2FA, language switcher, PWA install prompt)
- [x] Dashboard skeleton (KPI cards, charts placeholder, recent transactions, quick actions)
- [x] JWT Auth HttpModule (validates every request, 401 on AJAX, redirect on pages)
- [x] AJAX handlers: Auth, Translations, SyncPush, SyncPull, Dashboard, UserPreference
- [x] Design system (CSS components: cards, buttons, tables, modals, badges, toasts, skeleton loaders)
- [x] Seed data: 8 languages, subscription plans, core permissions
- [x] Translation seeds (EN, SO, AR, FR for Auth, Navigation, Common, Admin, Sync)

---

## Setup Instructions

### 1. Prerequisites

- SQL Server 2019 or Azure SQL
- IIS or IIS Express with .NET 4.8
- Visual Studio 2019+ or VS Code

### 2. Database Setup

```sql
-- 1. Create the database
CREATE DATABASE EidUbahleDB;

-- 2. Run the schema
-- Execute: App_Data/Schema_Phase1.sql

-- 3. Seed translations
-- Execute: App_Data/Seeds_Translations.sql
```

### 3. Configuration

Edit `Web.config`:

```xml
<!-- Connection string -->
<add name="DefaultConnection"
     connectionString="Server=YOUR_SERVER;Database=EidUbahleDB;User Id=YOUR_USER;******;..."
     providerName="System.Data.SqlClient" />

<!-- IMPORTANT: Change this secret in production! -->
<add key="Jwt:SecretKey" value="YOUR-64-CHARACTER-RANDOM-SECRET-HERE" />

<!-- Environment -->
<add key="App:Environment" value="Production" />
```

### 4. Create First Super Admin

Run `App_Data/Schema_Phase1.sql` then insert a tenant, company, branch, and user with your desired credentials. Use `PasswordService.HashPassword(password)` to generate the password hash.

### 5. Run the Application

```
iisexpress /path:C:\path\to\EidUbahle /port:5000
```

Navigate to `http://localhost:5000`

---

## Architecture Overview

```
EidUbahle/
├── App_Code/
│   ├── Domain/
│   │   ├── Entities/       ← BaseEntity, CoreEntities (Tenant, User, Role, Translation, Sync...)
│   │   ├── Enums/          ← All system enumerations
│   │   └── DTOs/           ← Data transfer objects for all layers
│   ├── BusinessLogic/      ← (Phase 3+) Accounting engine, workflow, rules
│   ├── Repositories/       ← (Phase 3+) Data access layer
│   ├── Services/
│   │   └── AuthService.cs  ← Login, refresh, logout, permission loading
│   ├── Infrastructure/
│   │   ├── Security/       ← JwtService, PasswordService, ConfigHelper
│   │   ├── Localization/   ← TranslationService (server-side)
│   │   └── Caching/        ← AppMemoryCache (in-memory, thread-safe)
│   └── CrossCutting/
│       └── ServiceLocator  ← Simple DI container for WebForms
├── MasterPages/
│   ├── Classic.master      ← Layout 1: Full sidebar
│   ├── TopNav.master       ← Layout 2: Top navigation bar
│   └── Compact.master      ← Layout 3: Slim header + icon sidebar
├── Pages/
│   ├── Login.aspx          ← JWT login, 2FA, offline, language switcher
│   ├── Dashboard.aspx      ← KPI cards, charts, quick actions
│   └── Error.aspx
├── Admin/
│   └── Translations.aspx   ← Language management, inline edit, import/export
├── Handlers/
│   ├── Auth.ashx           ← Login / refresh / logout
│   ├── Translations.ashx   ← Bundle serving + CRUD
│   ├── SyncPush.ashx       ← Offline → cloud sync (push)
│   ├── SyncPull.ashx       ← Cloud → offline sync (pull)
│   ├── Dashboard.ashx      ← KPI data
│   └── UserPreference.ashx ← Theme/language/layout persistence
├── Scripts/
│   ├── app.js              ← Main app bootstrap (init, nav, toasts, shortcuts)
│   ├── modules/
│   │   ├── i18n.js         ← Translation engine (client-side)
│   │   └── theme.js        ← Theme engine (dark/light/accent/layout)
│   ├── offline/
│   │   └── db.js           ← Dexie.js schema + IndexedDB helpers
│   └── sync/
│       └── sync-engine.js  ← Offline sync (push/pull/conflict/retry)
├── Styles/
│   ├── main.css            ← Design system (layout, components, utilities)
│   ├── themes.css          ← CSS custom property defaults
│   └── components.css      ← Extended components (tables, forms, wizard...)
├── Security/
│   └── JwtAuthModule.cs    ← HTTP Module: validates JWT on every request
├── App_Data/
│   ├── Schema_Phase1.sql   ← Complete MSSQL schema
│   └── Seeds_Translations.sql
├── sw.js                   ← Service Worker (offline, background sync, push)
├── manifest.webmanifest    ← PWA manifest
├── Global.asax             ← App startup, DI initialization
└── Web.config              ← Config (connection string, JWT, security headers)
```

---

## Key Design Decisions

1. **Offline-First**: Every read/write hits IndexedDB first; sync queue handles cloud propagation.
2. **JWT without libraries**: Pure HS256 in `JwtService.cs` – no external dependencies.
3. **No ScriptManager / UpdatePanels**: All async is XHR + JSON via `.ashx` handlers.
4. **Zero hardcoded strings**: All UI text goes through `i18n.t('key')`.
5. **Decoupled BLL**: `AuthService`, `TranslationService` are pure C# classes – no WebForms dependency. Future REST API needs no rewrites.
6. **CSS custom properties**: Theme switching is instant with zero JS DOM manipulation.

---

## Next Phase

**Phase 2: Multi-Tenancy, Company, Branch, Users & Roles**
- RBAC permission matrix
- User management UI (invite, roles, branch assignment)
- Tenant onboarding wizard
- Offline user sessions

---

*Built by EidUbahle – Enterprise Accounting SaaS Platform*
