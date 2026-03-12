---
name: unity-module-structure
description: ABP module architecture and layering rules for Unity. Use when creating new modules, adding cross-module dependencies, or understanding project organization and dependency direction.
---

# Unity Module Architecture

## Module Layout

Each ABP module follows a standard layered structure under `modules/`:

```
Unity.{ModuleName}/
  src/
    Unity.{ModuleName}.Domain.Shared/     ← Enums, constants, localization, ETOs
    Unity.{ModuleName}.Domain/            ← Entities, repository interfaces, domain services
    Unity.{ModuleName}.Application.Contracts/ ← DTOs, app service interfaces
    Unity.{ModuleName}.Application/       ← App service implementations, AutoMapper profiles
    Unity.{ModuleName}.EntityFrameworkCore/ ← DbContext, migrations (if module has own DB tables)
    Unity.{ModuleName}.HttpApi/           ← REST controllers
    Unity.{ModuleName}.HttpApi.Client/    ← Remote client proxies
    Unity.{ModuleName}.Web/               ← Razor Pages, view components
  test/
    Unity.{ModuleName}.TestBase/
    Unity.{ModuleName}.Application.Tests/
    Unity.{ModuleName}.Domain.Tests/
    Unity.{ModuleName}.EntityFrameworkCore.Tests/
```

Not all modules have every layer. Simpler modules may only have `Application`, `Application.Contracts`, `Shared`, and `Web`.

## Current Modules

| Module | Layers Present | Purpose |
|--------|---------------|---------|
| **Unity.Flex** | Shared, App.Contracts, App, Web, Tests | Dynamic forms/worksheets |
| **Unity.Notifications** | Full stack (Domain→Web, HttpApi, EF) | Email/messaging |
| **Unity.Payments** | Shared, App.Contracts, App, Web, Tests | Financial transactions |
| **Unity.Reporting** | Shared, App.Contracts, App, Web, Tests | Analytics & reports |
| **Unity.AI** | Shared, App.Contracts, App, Web | AI analysis (OpenAI) |
| **Unity.TenantManagement** | App.Contracts, App, HttpApi, Web, Tests | Multi-tenant admin |
| **Unity.Identity.Web** | Web, Tests | OIDC authentication UI |
| **Unity.Theme.UX2** | Theme package, Tests | Custom Razor Pages theme |
| **Unity.SharedKernel** | Single project | Cross-cutting utilities |

## Dependency Direction (Strict)

```
Web → HttpApi → Application.Contracts
Application → Domain + Application.Contracts
Domain → Domain.Shared
EntityFrameworkCore → Domain only
```

### Rules

- Web/HttpApi must NEVER depend on Application (only Application.Contracts).
- Application must NEVER depend on Web or EF Core.
- Domain must NEVER depend on Application, Web, or EF Core.
- Domain.Shared must have NO dependencies on other layers.
- EF Core must ONLY depend on Domain.

## ABP Module Classes

Every package has exactly one `AbpModule` class with `[DependsOn]` attributes.

```csharp
[DependsOn(
    typeof(GrantManagerDomainModule),
    typeof(AbpEntityFrameworkCoreModule)
)]
public class GrantManagerEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<GrantManagerDbContext>(options =>
        {
            options.AddDefaultRepositories();
        });
    }
}
```

## Multi-Tenancy

- The system uses ABP multi-tenancy with separate database per tenant.
- `GrantManagerDbContext` = host context, `GrantTenantDbContext` = tenant context.
- Tenant-scoped data is accessed via `ICurrentTenant` / tenant switching.
- The `Unity.TenantManagement` module handles tenant administration.

## Adding a New Feature

1. Identify which module the feature belongs to.
2. Add entities/repositories in Domain layer.
3. Add DTOs/interfaces in Application.Contracts.
4. Implement app services in Application.
5. Add EF Core configuration if new tables are needed.
6. Add UI in Web layer.
7. Add tests in the module's test projects.
8. Register the module class with `[DependsOn]`.
9. Run `dotnet build Unity.GrantManager.sln` and `dotnet test Unity.GrantManager.sln` to verify.

## Localization

Each module with Domain.Shared has its own localization under:
`src/Unity.{ModuleName}.Domain.Shared/Localization/{ModuleName}/en.json`

Use `L["Key"]` in application services and pages. All user-facing text must be localized.

## Permissions

Define in `*PermissionDefinitionProvider` in Application.Contracts.
Permission names follow `{ModuleName}.{Resource}.{Action}` convention.
