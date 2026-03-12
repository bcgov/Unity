---
applyTo: "**/*.cs"
description: "C# and .NET 9 development standards for ABP Framework 9.1.3"
---

# C# Conventions for Unity Grant Manager

- Target framework: .NET 9.0 with `<LangVersion>latest</LangVersion>`.
- Nullable reference types are enabled in most projects.
- This is an ABP Framework project. Use ABP base classes, not raw ASP.NET Core.
- This is NOT the Unity game engine. Do not suggest UnityEngine APIs.

## ABP Base Classes

- Application Services: Inherit `ApplicationService`, implement interface from Application.Contracts
- Domain Services: Inherit `DomainService`, use `Manager` suffix
- Entities: Inherit `FullAuditedAggregateRoot<TKey>` or `AuditedAggregateRoot<TKey>`
- API Controllers: Inherit `AbpController`
- Repositories: Use `IRepository<TEntity, TKey>` by default; custom only when needed

### Injected Properties Available in Base Classes

These properties are pre-injected in `ApplicationService`, `DomainService`, and `AbpController`:

| Property | Purpose |
|---|---|
| `GuidGenerator` | Create new entity IDs — never use `Guid.NewGuid()` |
| `Clock` | Use `Clock.Now` — never use `DateTime.Now` or `DateTime.UtcNow` |
| `CurrentUser` | Access authenticated user (Id, Name, Email, Roles) |
| `CurrentTenant` | Access current tenant context (Id, Name) |
| `L` / `L["Key"]` | Localization shortcut |
| `ObjectMapper` | AutoMapper-based mapping |
| `Logger` | Structured logging via `ILogger<T>` |
| `AuthorizationService` | Programmatic authorization checks |
| `UnitOfWorkManager` | Manual unit-of-work control |

## Dependency Injection

- ABP auto-registers services using marker interfaces — do NOT manually call `services.AddScoped<>()`
- `ITransientDependency` — new instance per injection
- `ISingletonDependency` — single shared instance
- `IScopedDependency` — one per request
- Application services, domain services, and repositories are auto-registered by ABP

## Entities & Domain

- Entities use rich domain model: private/protected setters, behaviour via methods.
- Include `protected` parameterless constructor for EF Core deserialization.
- Do not generate `Guid` keys inside constructors; accept `id` from `IGuidGenerator`.
- Reference other aggregate roots by Id only, not navigation properties.
- Domain services use `*Manager` suffix.
- Throw `BusinessException` with namespaced error codes for rule violations.

## Application Services

- Interface naming: `I*AppService` inheriting `IApplicationService`.
- All methods `async`, name ends with `Async`.
- Accept/return DTOs only, never entities. Define DTOs in `*.Application.Contracts`.
- Make all public methods `virtual`.
- Use **AutoMapper** (`ObjectMapper.Map<>()`) for DTO mapping. Do NOT use Mapperly.
- Mapping profiles: `*AutoMapperProfile.cs` inheriting `Profile`.

## Code Style

- 4 spaces indentation, no tabs
- No emojis in comments
- Always use braces, even for single-line statements
- Use `nameof` instead of string literals when referring to member names
- Prefer pattern matching and switch expressions where appropriate
- All user-facing text must be localized via `L["Key"]`. No hardcoded English strings.
- Permissions defined in `*PermissionDefinitionProvider` in Application.Contracts.
- Do not call other application services within the same module; push shared logic to domain services.

## Naming Conventions

- Follow PascalCase for public members, types, and methods
- Use camelCase for private fields and local variables
- Prefix interface names with `I`
- Domain Services: `*Manager` suffix (e.g., `AssessmentManager`)
- Application Services: `*AppService` suffix (e.g., `ApplicationAppService`)
- DTOs: Descriptive suffixes (`CreateApplicationDto`, `UpdateApplicationDto`, `ApplicationDto`)
- Event Transfer Objects: `*Eto` suffix for distributed events


## DTOs vs Entities

- Application services MUST accept and return DTOs only, never entities
- Use `ObjectMapper` (AutoMapper) to map between entities and DTOs
- Define mapping profiles in `*AutoMapperProfile` class in Application project

## Authorization

- Apply `[Authorize(PermissionName)]` attributes on application service methods
- Define permissions in `*Permissions` static class in Domain.Shared project

## Multi-Tenancy

- Tenant entities MUST implement `IMultiTenant` interface
- NEVER manually filter by `TenantId` — ABP handles this automatically
- Use `GrantTenantDbContext` for tenant data, `GrantManagerDbContext` for host data

## Error Handling

- Use `BusinessException` for domain-level errors with namespaced error codes (e.g., `"GrantManager:ApplicationNotFound"`)
- Map error codes to localization keys for user-friendly messages
- Use `.WithData("key", value)` for localized message interpolation
- Catch specific exception types, not generic `Exception`

## Common Mistakes to Avoid

- Don't expose entities from application services — always return DTOs
- Don't put business logic in application services — use domain services
- Don't create custom repositories unnecessarily — use generic `IRepository<T, TKey>` first
- Don't mix host and tenant data in same DbContext
- Don't ignore nullable warnings — fix them properly
- Don't use `DateTime.Now` — use `Clock.Now` or inject `IClock`
- Don't use `Guid.NewGuid()` — use `GuidGenerator.Create()`
- Don't use `services.AddScoped<>()` for ABP services — use marker interfaces
- Don't call application services from within the same module — extract shared logic to a domain service
- Don't embed entity name in app service methods — use `GetAsync`, not `GetApplicationAsync`
