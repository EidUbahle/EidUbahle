# Database Schemas

The `database/` folder documents SQL Server setup for CentralIdentity.

## Layout

- `migrations/` - ordered scripts used to create and evolve the database
- `schemas/` - schema-focused reference files for important tables and domains
- `seeds/` - optional bootstrap data scripts

## Migration sequence

Apply the migration scripts in version order:

1. `V001__Create_Database.sql`
2. `V002__Create_SchemaVersions_Table.sql`
3. `V003__Identity_Core.sql`
4. `V004__Sessions_RefreshTokens.sql`
5. `V005__Roles_Permissions.sql`
6. `V006__MFA.sql`

## Functional areas

- core identities and client applications
- user/application assignments
- authorization codes, sessions, and refresh tokens
- roles and permissions
- MFA methods and recovery codes
- audit logging
