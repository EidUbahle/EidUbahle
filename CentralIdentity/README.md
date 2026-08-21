# Central Identity & Authentication Platform

## Project overview

CentralIdentity is an ASP.NET Core 8 identity platform for centralized authentication, OAuth 2.0/OpenID Connect-style token issuance, application-scoped authorization, session management, RBAC, MFA, audit logging, and inactivity enforcement across multiple client systems.

## Requirements

### SDK

- .NET SDK 8.x

### Database

- Microsoft SQL Server
- Run the SQL scripts in `database/migrations/` in order
- Optional reference data: `database/seeds/001_Initial_Data.sql`

### External services

- No third-party hosted identity dependency is required
- A persistent RSA private key file is required for production token signing
- A 32-byte Base64 MFA encryption key is required for durable MFA secret storage in production

## Solution structure

```text
CentralIdentity/
├── CentralIdentity.slnx
├── src/
│   ├── CentralIdentity.Api/             # HTTP API, middleware, auth, rate limiting, hosted services
│   ├── CentralIdentity.Application/     # Use-case services and option binding
│   ├── CentralIdentity.Contracts/       # Request/response DTOs
│   ├── CentralIdentity.Domain/          # Entities, results, domain exceptions
│   └── CentralIdentity.Infrastructure/  # SQL repositories, hashing, JWT keys, MFA crypto
├── tests/
│   ├── CentralIdentity.UnitTests/
│   └── CentralIdentity.IntegrationTests/
├── database/
│   ├── migrations/
│   ├── schemas/
│   └── seeds/
├── docs/
├── deployment/
└── SECURITY-CHECKLIST.md
```

## Configuration

Use `src/CentralIdentity.Api/appsettings.example.json` as the template. Do not commit production secrets.

### Required configuration sections

- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer`
- `Jwt:SigningKeyId`
- `Jwt:RsaPrivateKeyPemFile`
- `OAuth:AuthorizationCodeLifetimeMinutes`
- `Security:ApplicationInactivityDays`
- `Security:MaxFailedLoginAttempts`
- `Security:LockoutMinutes`
- `Security:MfaEncryptionKey`
- `Cors:AllowedOrigins`

### Environment variables / secret guidance

Recommended production overrides:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Issuer`
- `Jwt__SigningKeyId`
- `Jwt__RsaPrivateKeyPemFile`
- `Security__MfaEncryptionKey`
- `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ...

### Secrets rules

- Never store real connection strings, private keys, certificates, client secrets, or MFA encryption keys in source control
- Keep the RSA private key outside the repository
- Generate `Security:MfaEncryptionKey` as a 32-byte Base64 value

## Build commands

```bash
cd CentralIdentity
dotnet restore
dotnet build --no-restore
```

## Test commands

```bash
cd CentralIdentity
dotnet test
```

Security/package audit commands used during finalization:

```bash
dotnet list CentralIdentity.slnx package --vulnerable --include-transitive
```

## Security controls

- RS256 JWT signing with JWKS exposure of the public key only
- PKCE enforced for authorization-code flow
- Exact redirect URI matching
- Per-request bearer-token validation of issuer, signature, lifetime, audience, user/application assignment, active session, and security stamp
- Application management endpoints locked behind authenticated administrator access
- Self-service profile access limited to the owning user or an administrator
- PBKDF2-HMAC-SHA512 password hashing (310,000 iterations)
- PBKDF2-HMAC-SHA256 client secret hashing (100,000 iterations)
- Refresh-token hashing, rotation, family reuse detection, and session revocation
- TOTP MFA with AES-GCM encrypted secret storage and hashed recovery codes
- Rate limiting for login, token, and MFA endpoints
- HSTS in production, CORS allow-listing, and hardened security headers
- Audit logging for security-sensitive events without logging secrets or tokens
- Per-application inactivity revocation worker

## Deployment instructions

1. Provision SQL Server and apply `database/migrations/V001__Create_Database.sql` through `V006__MFA.sql`
2. Populate `appsettings` via environment variables or secret store
3. Provide a persistent RSA private key file for `Jwt:RsaPrivateKeyPemFile`
4. Provide a 32-byte Base64 `Security:MfaEncryptionKey`
5. Set explicit `Cors:AllowedOrigins`
6. Publish and deploy the API:

```bash
cd CentralIdentity
dotnet publish src/CentralIdentity.Api -c Release -o publish
```

Additional deployment guidance:

- `docs/DEPLOYMENT.md`
- `deployment/IIS-DEPLOYMENT.md`

## Troubleshooting

- **NETSDK1064 during `dotnet clean`**: remove stale `bin/` and `obj/` directories, then run `dotnet restore`
- **401 on protected endpoints**: verify issuer, RSA signing key, audience, active session, and user/application assignment
- **403 on management endpoints**: the token must resolve to an administrator role for the target application
- **MFA decryption failures after restart**: ensure `Security:MfaEncryptionKey` is configured and stable across deployments
- **Redirect URI rejected**: confirm the URI exactly matches the registered value
- **Cross-origin calls blocked**: populate `Cors:AllowedOrigins` with the precise client origins
