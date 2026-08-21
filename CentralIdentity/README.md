# Central Identity & Authentication Platform

CentralIdentity is a production-oriented ASP.NET Core 8 platform for centralized authentication, authorization, session management, and application-scoped access control across multiple client systems such as University, Hospital, and Tax applications.

## Platform Overview

The platform provides:

- OAuth 2.0 / OpenID Connect-style authorization code flow with mandatory PKCE
- RS256 JWT access tokens with JWKS discovery
- Refresh token rotation and reuse detection
- Per-application user assignments and session isolation
- Roles and permissions
- TOTP MFA with encrypted secrets and hashed recovery codes
- Audit logging, rate limiting, security headers, and inactivity enforcement

## Solution Structure

```text
CentralIdentity/
├── CentralIdentity.slnx
├── src/
│   ├── CentralIdentity.Api/
│   ├── CentralIdentity.Application/
│   ├── CentralIdentity.Contracts/
│   ├── CentralIdentity.Domain/
│   └── CentralIdentity.Infrastructure/
├── tests/
│   ├── CentralIdentity.UnitTests/
│   └── CentralIdentity.IntegrationTests/
├── database/
│   ├── migrations/
│   ├── schemas/
│   └── seeds/
├── docs/
├── deployment/
├── SECURITY-CHECKLIST.md
└── README.md
```

## Architecture

The solution follows Clean Architecture:

- **API**: HTTP endpoints, middleware, rate limiting, health checks, Swagger, hosted services
- **Application**: use-case services and interfaces
- **Domain**: entities and core business rules
- **Infrastructure**: SQL Server repositories, cryptography, key loading, and external integrations
- **Contracts**: API DTOs

More detail: `docs/ARCHITECTURE.md`

## OAuth2 / OIDC Flows

Supported flows:

- **authorization_code + PKCE** for web and mobile clients
- **refresh_token** for renewing access without reauthentication

Main endpoints:

- `GET /.well-known/openid-configuration`
- `GET /.well-known/jwks.json`
- `GET /connect/authorize`
- `POST /connect/token`
- `POST /connect/revoke`
- `GET /connect/userinfo`

Flow detail: `docs/AUTHENTICATION-FLOW.md`

## JWT / JWKS

- Access tokens are signed with **RS256**
- Issuer is configured under `Jwt:Issuer`
- Public keys are published through JWKS
- Private key material stays outside source control
- Token validation includes issuer, audience, signature, and lifetime

## Refresh Tokens & Sessions

- Refresh tokens are application-scoped and session-bound
- Rotation occurs on every successful refresh
- Reuse detection revokes the entire token family and session
- Logout and explicit revocation invalidate server-side sessions and their refresh tokens

Lifecycle detail: `docs/TOKEN-LIFECYCLE.md`

## Application-Scoped Authorization

Each user is assigned to one or more applications through `IdentityUserApplication`.

This enables:

- access for one user across multiple client applications
- isolated revocation per application
- per-application inactivity enforcement
- token/session isolation between applications

Example:

- Revoking Hospital access does **not** revoke University or Tax access
- University refresh tokens cannot be reused for Hospital sessions

## Roles & Permissions

The platform includes role and permission data models and repositories for:

- creating roles
- assigning roles to users
- attaching permissions to roles
- resolving effective access per user

This supports centralized authorization beyond basic authentication.

## MFA

MFA support includes:

- TOTP setup and verification
- encrypted MFA secret storage
- hashed recovery codes
- MFA enable/disable and regeneration flows

Integration guidance: `docs/MAUI-INTEGRATION.md` and `docs/CLIENT-INTEGRATION.md`

## 7-Day Inactivity Enforcement

A hosted background service scans active user/application assignments and revokes any assignment inactive for longer than the configured threshold:

- default threshold: **7 days**
- configurable batch size and interval
- revocation is **per application**, not global per user

## Security Features

- PBKDF2 password hashing (**310,000** iterations, SHA-512)
- PBKDF2 client secret hashing (**100,000** iterations, SHA-256)
- hashed refresh tokens
- RS256 asymmetric token signing
- exact redirect URI matching
- mandatory PKCE
- rate limiting for login, token, and MFA endpoints
- HSTS in production
- CORS allow-listing
- secure response headers
- global exception handling
- audit logging of security events

Checklist: `SECURITY-CHECKLIST.md`

## Testing

Test projects:

- `tests/CentralIdentity.UnitTests`
- `tests/CentralIdentity.IntegrationTests`

Coverage areas include:

- password and client secret hashing
- PKCE and authorization code validation
- JWT generation and validation
- refresh token rotation and reuse protection
- RBAC
- MFA
- security headers and protected endpoints
- inactivity enforcement
- end-to-end phase validation

Run locally:

```bash
cd CentralIdentity
dotnet build
dotnet test --no-build
```

## Configuration

Use `src/CentralIdentity.Api/appsettings.example.json` as the production template.

Important sections:

- `ConnectionStrings:DefaultConnection`
- `Jwt`
- `OAuth`
- `Security`
- `Cors`
- `Serilog`

Do not commit production secrets, RSA private keys, certificates, or environment-specific appsettings files.

## Database

- migration scripts live under `database/migrations/`
- schema notes live under `database/schemas/`
- reference data scripts live under `database/seeds/`

Start with:

- `database/schemas/README.md`
- `database/seeds/001_Initial_Data.sql`

## Client Integration

- ASP.NET Core Razor Pages guidance: `docs/CLIENT-INTEGRATION.md`
- .NET MAUI guidance: `docs/MAUI-INTEGRATION.md`

## Deployment

- deployment overview: `docs/DEPLOYMENT.md`
- IIS procedure: `deployment/IIS-DEPLOYMENT.md`

## Getting Started

```bash
cd CentralIdentity
dotnet restore
dotnet build
dotnet test --no-build
dotnet run --project src/CentralIdentity.Api
```
