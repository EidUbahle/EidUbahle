# ASP.NET Core Razor Pages Client Integration

Use **Authorization Code + PKCE** for server-rendered applications.

## Recommended setup

- Register the client application in CentralIdentity with:
  - exact redirect URIs
  - application audience
  - client type `Confidential`
- Store the generated client secret outside source control.

## Typical flow

1. Redirect unauthenticated users to `/connect/authorize`.
2. Handle the callback and exchange the authorization code at `/connect/token`.
3. Store tokens in an encrypted server-side session or secure authentication cookie.
4. Attach the access token when calling protected APIs.
5. Use the refresh token to silently renew access when required.
6. Call CentralIdentity logout/revocation endpoints when the local session ends.

## Important notes

- Keep redirect URI matching exact.
- Treat the refresh token as an application-scoped credential.
- If the user's access to one application is revoked, tokens for other applications remain valid.
