# Deployment Overview

CentralIdentity is intended to run behind **HTTPS** with externalized secrets and SQL Server connectivity.

## Production requirements

- .NET 8 Hosting Bundle on Windows Server
- IIS or another reverse proxy
- SQL Server connectivity
- RSA private key stored outside the repository
- Production values supplied through environment variables, secure files, or a secrets vault

## Deployment checklist

1. Publish the API in Release mode.
2. Apply database migration scripts in order.
3. Provide production configuration from `appsettings.example.json`.
4. Configure HTTPS and HSTS.
5. Restrict CORS to approved origins.
6. Validate `/.well-known/openid-configuration`, JWKS, token, revoke, userinfo, and health endpoints.

See `deployment/IIS-DEPLOYMENT.md` for a concrete IIS procedure.
