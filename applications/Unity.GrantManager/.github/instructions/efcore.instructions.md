---
applyTo: "**/EntityFrameworkCore/**/*.cs"
---

# EF Core Conventions for Unity Grant Manager

- Provider: **Npgsql** (PostgreSQL 17).
- Two database contexts: `GrantManagerDbContext` (host) and `GrantTenantDbContext` (tenant).
- Entity configuration is done inline in `OnModelCreating` of `GrantManagerDbContext` and `GrantTenantDbContext`.
- When configuring entities, follow ABP conventions (e.g., table naming, key configuration) consistently.
- Use `options.AddDefaultRepositories(includeAllEntities: true)` in `GrantManagerEntityFrameworkCoreModule`.
- Prefer ABP's generated default repositories; add custom repositories only when additional behavior is required.
- Tests use **SQLite in-memory** databases, not PostgreSQL.

## Migrations

Always specify the context when adding migrations:

```bash
# Host migrations
dotnet ef migrations add <Name> --context GrantManagerDbContext --output-dir Migrations/HostMigrations

# Tenant migrations
dotnet ef migrations add <Name> --context GrantTenantDbContext --output-dir Migrations/TenantMigrations
```
