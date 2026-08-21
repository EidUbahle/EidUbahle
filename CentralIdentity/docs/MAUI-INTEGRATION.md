# .NET MAUI Integration

.NET MAUI clients should use **Authorization Code + PKCE** as a **public client**.

## Guidance

- Do **not** embed a client secret in the mobile app.
- Register platform-specific redirect URIs.
- Generate a high-entropy PKCE verifier per sign-in attempt.
- Exchange the authorization code at `/connect/token` with the verifier.

## Token handling

- Store refresh tokens using platform-secure storage.
- Keep access tokens in memory where possible.
- Use refresh tokens only for the same registered application.
- Handle `invalid_grant` by forcing the user through sign-in again.

## Logout and revocation

- Clear local secure storage on logout.
- If the server revokes the mobile app assignment, refresh attempts must stop and the user must reauthenticate after access is restored.
