# Authentication Flow

## Authorization Code + PKCE

1. The client redirects the user to `/connect/authorize` with `response_type=code`, `client_id`, `redirect_uri`, `scope`, `code_challenge`, and `code_challenge_method=S256`.
2. CentralIdentity validates the client, redirect URI, user, and the user's assignment to the requested application.
3. A single-use authorization code is created and returned to the redirect URI.
4. The client posts the code to `/connect/token` with `grant_type=authorization_code`, `client_id`, `redirect_uri`, and `code_verifier`.
5. Confidential clients also send `client_secret`; public clients use PKCE without a secret.
6. The platform validates the authorization code and PKCE verifier, then issues:
   - a short-lived RS256 access token
   - a refresh token
   - a server-side session identifier

## UserInfo

Clients call `/connect/userinfo` with the bearer access token. The API validates signature, issuer, audience, lifetime, user status, and client status before returning claims.
