---
name: unity-ef-core
description: ABP Entity Framework Core for Unity - DbContext configuration, entity mapping, repository implementation, EF migrations. Use when working in EntityFrameworkCore projects, adding migrations, or implementing repositories.
---

# Unity EF Core Patterns

> This project uses EF Core 9.0 with PostgreSQL 17 (Npgsql). Tests use SQLite in-memory.

## Database Contexts

This project has **two distinct database contexts**:

| Context | Purpose | Migrations Directory |
|---------|---------|---------------------|
| `GrantManagerDbContext` | Host/shared system tables | `Migrations/HostMigrations` |
| `GrantTenantDbContext` | Per-tenant isolated data | `Migrations/TenantMigrations` |

Always specify the context when adding migrations:

```bash
cd src/Unity.GrantManager.EntityFrameworkCore

# Host migration
dotnet ef migrations add <Name> --context GrantManagerDbContext --output-dir Migrations/HostMigrations

# Tenant migration
dotnet ef migrations add <Name> --context GrantTenantDbContext --output-dir Migrations/TenantMigrations
```

## Entity Configuration

Entity mapping is done via extension methods on `ModelBuilder`, NOT inline in `OnModelCreating`.

```csharp
public static class GrantManagerDbContextModelCreatingExtensions
{
    public static void ConfigureGrantManager(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<Grant>(b =>
        {
            b.ToTable(GrantManagerConsts.DbTablePrefix + "Grants", GrantManagerConsts.DbSchema);
            b.ConfigureByConvention(); // Always call this first

            b.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(GrantConsts.MaxNameLength);

            b.HasIndex(x => x.Name);
        });
    }
}
```

**Rules:**
- Always call `b.ConfigureByConvention()` for every entity.
- Use table prefix from constants (not hardcoded).
- Default schema should be `null`.

## Repository Implementation

```csharp
public class GrantRepository : EfCoreRepository<GrantManagerDbContext, Grant, Guid>, IGrantRepository
{
    public GrantRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider)
        : base(dbContextProvider) { }

    public async Task<Grant?> FindByNameAsync(
        string name,
        bool includeDetails = true,
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .IncludeDetails(includeDetails)
            .FirstOrDefaultAsync(g => g.Name == name, GetCancellationToken(cancellationToken));
    }
}
```

- Use DbContext interface as generic parameter.
- Pass cancellation tokens via `GetCancellationToken(cancellationToken)`.
- Use `IncludeDetails()` extensions per aggregate root.

## Module Registration

```csharp
context.Services.AddAbpDbContext<GrantManagerDbContext>(options =>
{
    options.AddDefaultRepositories(); // Aggregate roots only, NOT includeAllEntities: true
});

Configure<AbpDbContextOptions>(options =>
{
    options.UseNpgsql(); // PostgreSQL
});
```

## Never Do

| Don't | Do Instead |
|-------|-----------|
| `AddDefaultRepositories(includeAllEntities: true)` | `AddDefaultRepositories()` — aggregate roots only |
| Skip `ConfigureByConvention()` | Always call it first in entity config |
| Inject DbContext in app/domain services | Use `IRepository<T>` or custom repository interface |
| Use lazy loading | Explicit `.Include()` via `IncludeDetails()` |

## Migrations .editorconfig

The `Migrations/` folder has its own `.editorconfig` suppressing analyzer warnings (S1128, S1192, CS8981, CA1861, IDE naming rules). This is intentional — do not modify migration files for style.
