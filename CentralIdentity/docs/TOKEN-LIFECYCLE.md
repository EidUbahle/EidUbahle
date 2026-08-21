# Token Lifecycle

## Access Tokens

- Signed with **RS256**
- Issued per user and per application
- Default lifetime: **10 minutes**
- Intended for API authorization only

## Refresh Tokens

- Stored as hashes in persistence
- Issued alongside the access token
- Default lifetime: **30 days**
- Bound to a session and application
- Rotated on refresh

## Refresh Flow

1. Client posts `grant_type=refresh_token` to `/connect/token`.
2. CentralIdentity validates the client and refresh token.
3. If valid, the old refresh token is revoked and a new access/refresh token pair is issued.
4. If a revoked refresh token is reused, the token family is revoked and the session is invalidated.

## Revocation

- Logout revokes the current session and its refresh tokens.
- User/application revocation invalidates sessions and refresh tokens for that application only.
- Inactivity enforcement revokes dormant application assignments without disabling the user globally.
