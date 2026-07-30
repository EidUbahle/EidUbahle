# EidUbahle Enterprise ERP

**Phase 1 Complete – System Architecture & Core Scaffold**

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
