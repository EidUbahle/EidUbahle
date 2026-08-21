# Production Security Checklist

Central Identity & Authentication Platform

## Infrastructure
- [ ] HTTPS enabled on all endpoints
- [ ] HSTS enabled in production
- [ ] Production secrets externalized (not in appsettings.json)

## Authentication & Authorization
- [ ] Client secrets hashed (PBKDF2)
- [ ] Passwords hashed (PBKDF2, 100,000 iterations)
- [ ] Refresh tokens hashed (SHA-256)
- [ ] Signing private key protected (not in repository)
- [ ] JWKS exposes public keys only
- [ ] JWT issuer validated on every request
- [ ] JWT audience validated on every request
- [ ] JWT signature validated (RS256 asymmetric)
- [ ] JWT lifetime validated (10 minute default)
- [ ] PKCE enforced for all authorization_code flows
- [ ] Redirect URI exact matching (no wildcard)

## Session & Token Security
- [ ] Refresh-token reuse detection enabled (family revocation on reuse)
- [ ] Refresh token rotation on every use
- [ ] Session revocation on logout
- [ ] Application-scoped session isolation (University ≠ Hospital ≠ Tax)
- [ ] Security stamp invalidation on password change/MFA reset

## Account Security
- [ ] Rate limiting enabled on login/token/MFA endpoints
- [ ] Account lockout enabled (default: 5 attempts, 15 min)
- [ ] MFA available (TOTP)
- [ ] MFA secret encrypted at rest (AES-256)
- [ ] Recovery codes hashed (SHA-256)

## Operational Security
- [ ] Audit logging enabled for all security events
- [ ] Sensitive values never logged (passwords, tokens, secrets, OTPs)
- [ ] CORS restricted to allow-list origins only
- [ ] Security headers set (X-Content-Type-Options, X-Frame-Options, Referrer-Policy)
- [ ] Global error handler prevents stack trace leakage
- [ ] 7-day per-application inactivity enforcement enabled

## Database Security
- [ ] Database backups configured
- [ ] Connection strings externalized
- [ ] SQL injection prevention (parameterized queries only — no string concatenation)

## Deployment
- [ ] RSA signing key rotation procedure documented
- [ ] Production appsettings not committed to repository
- [ ] Private keys not committed to repository
- [ ] Real certificates not committed to repository
