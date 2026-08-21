# IIS Deployment

## Prerequisites

- Windows Server with IIS
- .NET 8 Hosting Bundle installed
- Access to SQL Server
- External RSA private key file

## Steps

1. Publish the API:
   ```powershell
   dotnet publish .\src\CentralIdentity.Api\CentralIdentity.Api.csproj -c Release -o .\publish
   ```
2. Create an IIS site and application pool:
   - No managed code
   - HTTPS binding with a real certificate
3. Copy the publish output to the IIS site folder.
4. Provide production configuration:
   - connection string
   - JWT issuer
   - signing key id
   - RSA private key path
   - MFA encryption key
   - allowed CORS origins
5. Ensure the IIS identity can read the RSA key file and write application logs if file logging is enabled.
6. Run database migrations before first traffic.
7. Recycle the app pool and verify:
   - `/.well-known/openid-configuration`
   - `/.well-known/jwks.json`
   - `/health`
   - `/swagger` in non-production test environments only

## Hardening

- Disable directory browsing
- Enforce HTTPS redirection
- Keep production secrets out of the repository
- Rotate signing keys with a documented process
