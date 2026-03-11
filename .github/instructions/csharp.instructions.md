<!-- Inspired by: https://github.com/github/awesome-copilot/blob/main/instructions/csharp.instructions.md -->
---
applyTo: "**/*.cs"
description: "C# and .NET 9 development standards for ABP Framework 9.1.3"
---

# C# Development Standards

Apply the repository-wide guidance from `../copilot-instructions.md` to all C# code.

## Language & Framework

- Target .NET 9.0 with C# 12 features (primary constructors, collection expressions, etc.)
- Nullable reference types are ENABLED project-wide — always declare nullability explicitly
- Use `is null` or `is not null` instead of `== null` or `!= null`
- Use `null!` only when DI guarantees non-null (e.g., `DbSet<T>` properties)

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

## Entity Constructors

- Always include a `protected` parameterless constructor for EF Core deserialization
- Public constructor accepts `Guid id` from `IGuidGenerator` — never call `Guid.NewGuid()`
- Use `Check.NotNullOrWhiteSpace()` and `Check.NotNull()` for constructor validation
- Use internal/private setters to protect domain invariants

## Naming Conventions

- Follow PascalCase for public members, types, and methods
- Use camelCase for private fields and local variables
- Prefix interface names with `I`
- Domain Services: `*Manager` suffix (e.g., `AssessmentManager`)
- Application Services: `*AppService` suffix (e.g., `ApplicationAppService`)
- DTOs: Descriptive suffixes (`CreateApplicationDto`, `UpdateApplicationDto`, `ApplicationDto`)
- Event Transfer Objects: `*Eto` suffix for distributed events

## Method Conventions

- All public methods MUST be `virtual` for ABP extensibility
- Async methods MUST have `Async` suffix and use `async/await`
- Use `protected virtual` instead of `private` for helper methods
- Always specify access modifiers explicitly

## Code Style

- 4 spaces indentation, no tabs
- Always use braces, even for single-line statements
- Apply code-formatting style defined in `.editorconfig`
- Use `nameof` instead of string literals when referring to member names
- Prefer pattern matching and switch expressions where appropriate

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
- Ensure XML doc comments are created for public APIs

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
