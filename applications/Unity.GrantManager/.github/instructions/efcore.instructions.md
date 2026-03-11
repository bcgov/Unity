---
applyTo: "**/EntityFrameworkCore/**/*.cs"
---

# EF Core Conventions for Unity Grant Manager

- Provider: **Npgsql** (PostgreSQL 17).
- Two database contexts: `GrantManagerDbContext` (host) and `GrantTenantDbContext` (tenant).
- Entity configuration uses extension methods on `ModelBuilder` (`ConfigureMyProject()`), not inline in `OnModelCreating`.
- Always call `b.ConfigureByConvention()` for every entity mapping.
- Use `options.AddDefaultRepositories()` without `includeAllEntities: true`.
- Repository implementations inherit `EfCoreRepository<TDbContext, TEntity, TKey>`.
- Tests use **SQLite in-memory** databases, not PostgreSQL.

## Migrations

Always specify the context when adding migrations:

```bash
# Host migrations
dotnet ef migrations add <Name> --context GrantManagerDbContext --output-dir Migrations/HostMigrations

# Tenant migrations
dotnet ef migrations add <Name> --context GrantTenantDbContext --output-dir Migrations/TenantMigrations
```
