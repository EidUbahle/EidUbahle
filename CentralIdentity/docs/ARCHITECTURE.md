# Architecture

CentralIdentity is an ASP.NET Core 8 central identity platform organized with Clean Architecture:

- **CentralIdentity.Api** hosts controllers, middleware, DI, health checks, Swagger, rate limiting, security headers, and the inactivity background service.
- **CentralIdentity.Application** contains use-case services for users, applications, authorization codes, tokens, and per-application access control.
- **CentralIdentity.Domain** defines entities such as users, applications, sessions, refresh tokens, roles, permissions, MFA methods, and audit logs.
- **CentralIdentity.Infrastructure** implements SQL Server repositories, cryptography, JWT signing-key access, and other external integrations.
- **CentralIdentity.Contracts** contains request/response DTOs shared by the API surface.

Key security boundaries:

- OAuth clients are validated per application.
- Access is scoped to a specific user/application assignment.
- Sessions and refresh tokens are isolated per application.
- A background worker revokes inactive user/application access after 7 days.
