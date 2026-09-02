# WAMO Waste Management

WAMO Waste Management is a production-oriented ASP.NET Web Forms solution for managing waste services, customers, collections, complaints, shop orders, multilingual content, and security-sensitive administration workflows.

## Project Overview

The solution uses ASP.NET Web Forms, .NET Framework 4.7.2, C#, SQL Server, pure ADO.NET, Bootstrap 5, jQuery AJAX, WebMethods, and PWA assets.

## Repository Layout

```text
WamoApp/
├── WamoApp.sln
├── WamoApp/
│   ├── Admin/
│   ├── App_Code/
│   ├── App_Data/WamoDB.sql
│   ├── Content/
│   ├── PWA/
│   ├── *.aspx / *.cs
│   ├── Global.asax
│   └── Web.config
└── README.md
```

## Requirements

- Windows IIS with .NET Framework 4.7.2+
- SQL Server
- SMTP server
- HTTPS certificate for production

## Database Setup

1. Open `/home/runner/work/EidUbahle/EidUbahle/WamoApp/WamoApp/App_Data/WamoDB.sql` in SSMS.
2. Execute the script.
3. Update the `WamoDb` connection string in `/home/runner/work/EidUbahle/EidUbahle/WamoApp/WamoApp/Web.config`.

## Admin Login

- Username: `superadmin`
- Email: `admin@wamo.local`
- Temporary Password: `ChangeMe!123`

The seeded account has `MustChangePassword = 1`; change it immediately in production.

## Default Roles

- Super Admin
- Administrator
- Manager
- Collection Manager
- Collection Staff
- Finance
- Customer Support
- Shop Manager
- Content Manager
- Customer

## Security Architecture

- PBKDF2 password hashing with random salt
- DB-backed `UserSessions`
- CSRF header validation for AJAX writes
- Page-level and CRUD permission checks
- Parameterized ADO.NET queries only
- Secure cookies and security headers
- Audit logging for sensitive actions

## Session Management

Sessions are stored in `UserSessions`, remain active while in use, and are ended only by logout, revoke, expiry, or inactivity timeout.

## Permission System

`PermissionManager`, `BasePage`, and server-side WebMethod checks enforce page access and CRUD permissions.

## PWA Installation

Android/Chrome uses `beforeinstallprompt`; iOS shows Add to Home Screen instructions.

## Configuration

Update Web.config for:

- connection strings
- SMTP settings
- `RequireHttps`
- application URL
- cookie names
- inactivity timeout

## Deployment Checklist

- change seeded password
- replace SMTP placeholders
- enable HTTPS and secure cookies
- verify permissions, sessions, reset flow, and PWA install
- build the solution from a Windows/.NET Framework environment
