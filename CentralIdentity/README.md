# Central Identity & Authentication Platform

A production-oriented ASP.NET Core 8 Web API implementing Clean Architecture for central identity and authentication.

## Solution Structure

```
CentralIdentity/
├── CentralIdentity.sln
├── src/
│   ├── CentralIdentity.Api/           # Entry point — controllers, middleware, DI, configuration
│   ├── CentralIdentity.Application/   # Use cases, application services, interfaces
│   ├── CentralIdentity.Domain/        # Core domain entities, value objects, exceptions
│   ├── CentralIdentity.Infrastructure/# SQL Server / ADO.NET data access, external services
│   └── CentralIdentity.Contracts/     # Shared request/response DTOs (API contracts)
├── tests/
│   ├── CentralIdentity.UnitTests/     # Pure unit tests (xUnit)
│   └── CentralIdentity.IntegrationTests/ # Integration tests (WebApplicationFactory)
├── database/
│   ├── migrations/                    # Versioned SQL migration scripts (V001__, V002__, ...)
│   ├── schemas/                       # Schema object definitions
│   └── seeds/                         # Reference / seed data scripts
└── README.md
```

## Technology Stack

| Concern | Choice |
|---------|--------|
| Runtime | .NET 8 LTS |
| API framework | ASP.NET Core 8 Web API |
| Database | SQL Server |
| Data access | ADO.NET / raw SQL (no Entity Framework) |
| Identity | Custom (no ASP.NET Core Identity) |
| Logging | Serilog (Console + rolling File) |
| API docs | Swagger / Swashbuckle |
| Testing | xUnit + Microsoft.AspNetCore.Mvc.Testing |

## Phase 1 — What Is Implemented

- ✅ Solution structure following Clean Architecture
- ✅ Project references (Domain ← Application ← Infrastructure / Api)
- ✅ SQL Server connection factory using `Microsoft.Data.SqlClient`
- ✅ Global exception handler middleware (maps domain exceptions → HTTP problem details)
- ✅ Structured logging with Serilog (request logging, rolling file, bootstrap logger)
- ✅ Swagger / OpenAPI with XML comments
- ✅ HTTPS configuration (HSTS in production)
- ✅ CORS policy (configurable via `appsettings.json → Cors:AllowedOrigins`)
- ✅ Health check endpoint (`GET /api/health`) backed by a live SQL Server probe
- ✅ Database migration scripts skeleton
- ✅ Unit tests and integration tests

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local or remote)

## Getting Started

```bash
# 1. Restore & build
cd CentralIdentity
dotnet restore
dotnet build

# 2. Update connection string
# Edit src/CentralIdentity.Api/appsettings.Development.json

# 3. Run database migrations
# Execute database/migrations/*.sql scripts in order against SQL Server

# 4. Run the API
dotnet run --project src/CentralIdentity.Api

# 5. Run tests
dotnet test
```

## Configuration

Key settings in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=CentralIdentityDb;..."
  },
  "Cors": {
    "AllowedOrigins": ["https://yourdomain.com"]
  }
}
```

## Phase 2 & 3 (Implemented)

- User, Application, and User-Application management (CRUD) via raw ADO.NET repositories
- Password hashing with PBKDF2-HMAC-SHA512 (310,000 iterations); client secrets hashed with PBKDF2-HMAC-SHA256
- OAuth2 authorization_code grant with mandatory PKCE (S256), single-use authorization codes
- RS256-signed (asymmetric) JWT access tokens, `/.well-known/openid-configuration` and `/.well-known/jwks.json` discovery endpoints

## What Comes Next

- Refresh tokens
- Role / permission management
- Audit logging
- Interactive login/session UI (the current `/connect/authorize` endpoint identifies the user via a `user_id` parameter as a placeholder for a real session mechanism)
