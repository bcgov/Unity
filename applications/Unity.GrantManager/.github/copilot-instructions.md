# Unity Grant Manager – Copilot Instructions

> **Trust these instructions first.** Only search the codebase when information here is incomplete or incorrect.
> This is NOT the Unity game engine. Do not suggest UnityEngine APIs.

## Project Overview

Unity Grant Manager is a **grant management portal** for the Province of British Columbia, built on **ABP Framework 9.1.3** with **.NET 9.0**, targeting **PostgreSQL 17**. The UI uses **Razor Pages** with a custom ABP theme (Unity.Theme.UX2). The architecture follows ABP's **Domain-Driven Design (DDD)** layered module pattern.

**Key stack:** .NET 9 · ABP 9.1.3 · EF Core 9.0 · PostgreSQL 17 · Redis · RabbitMQ · xUnit · Shouldly · NSubstitute · AutoMapper · Cypress (E2E)

## Repository Layout

```
Unity.GrantManager.sln                    ← Solution file (63 projects)
common.props                              ← Shared MSBuild properties
Directory.Build.props                     ← Global build props (suppresses NU1701, MSB3277)
NuGet.Config                              ← NuGet package sources
.env.example                              ← Environment variable template
docker-compose.yml                        ← Docker dev environment
src/
  Unity.GrantManager.Web/                 ← Razor Pages web app (entry point)
  Unity.GrantManager.Application/         ← App services, AutoMapper profiles
  Unity.GrantManager.Application.Contracts/ ← DTOs, interfaces
  Unity.GrantManager.Domain/              ← Entities, repositories, domain services
  Unity.GrantManager.Domain.Shared/       ← Enums, constants, localization (en.json)
  Unity.GrantManager.EntityFrameworkCore/  ← DbContext, migrations, EF config
  Unity.GrantManager.HttpApi/             ← REST controllers
  Unity.GrantManager.HttpApi.Client/      ← Remote service client proxies
  Unity.GrantManager.DbMigrator/          ← Database migration console app
test/
  Unity.GrantManager.TestBase/            ← Shared test fixtures
  Unity.GrantManager.Application.Tests/   ← App service tests
  Unity.GrantManager.Domain.Tests/        ← Domain logic tests
  Unity.GrantManager.EntityFrameworkCore.Tests/
  Unity.GrantManager.Web.Tests/
modules/                                  ← ABP modules (each with src/ and test/)
  Unity.Flex/                             ← Dynamic forms/worksheets
  Unity.Notifications/                    ← Email/messaging (full-stack module)
  Unity.Payments/                         ← Financial transactions
  Unity.Reporting/                        ← Analytics & reports
  Unity.AI/                               ← AI-powered analysis (OpenAI)
  Unity.TenantManagement/                 ← Multi-tenant admin
  Unity.Identity.Web/                     ← OIDC authentication
  Unity.Theme.UX2/                        ← Custom Razor Pages theme
  Unity.SharedKernel/                     ← Cross-cutting utilities
```

## Build & Test Commands

All commands run from this directory (`applications/Unity.GrantManager/`).

### Restore & Build

```bash
dotnet restore Unity.GrantManager.sln
dotnet build Unity.GrantManager.sln --no-restore
```

- Build takes ~3 minutes. The solution has 63 projects.
- There is **1 expected warning** in `Unity.GrantManager.Web/Pages/Dashboard/Index.cshtml.cs` (CS8604 null reference). Do not try to fix it unless explicitly asked.
- `Directory.Build.props` suppresses NU1701 and MSB3277 warnings globally; do not re-add these suppressions in individual projects.
- `common.props` sets `<LangVersion>latest</LangVersion>` and suppresses CS1591.

### Run All Tests

```bash
dotnet test Unity.GrantManager.sln --no-build
```

- ~470 tests across 15 test projects. All use **xUnit** with **Shouldly** assertions and **NSubstitute** mocks.
- Tests use **SQLite in-memory** databases (not PostgreSQL). No database setup required.
- Test run takes ~1–2 minutes after build.
- To run a single test project: `dotnet test test/Unity.GrantManager.Application.Tests/ --no-build`

### EF Core Migrations

The solution has **two database contexts** — always specify which context:

```bash
cd src/Unity.GrantManager.EntityFrameworkCore

# Host migrations (shared/system tables)
dotnet ef migrations add <Name> --context GrantManagerDbContext --output-dir Migrations/HostMigrations

# Tenant migrations (per-tenant isolated data)
dotnet ef migrations add <Name> --context GrantTenantDbContext --output-dir Migrations/TenantMigrations
```

## CI Pipeline (PR Checks)

Every PR triggers branch-specific GitHub Actions workflows (in the repo root `.github/workflows/`) that:

1. **Validate source branch** — PRs to `dev` must come from `feature/*`, `hotfix/*`, `bugfix/*`, `test`, or `main`. PRs to `main` must come from `test` or `hotfix/*` only.
2. **Discover test projects** — Finds all `*Tests.csproj` files automatically.
3. **Run tests in parallel matrix** — Each test project runs independently with `dotnet test` using .NET 9.0.x.
4. **Aggregate results** — Posts pass/fail badge as PR comment.

Always ensure `dotnet build` and `dotnet test` pass before submitting changes.

## Architecture Rules

### ABP Module Layering (Dependency Direction)

```
Web → HttpApi → Application.Contracts
Application → Domain + Application.Contracts
Domain → Domain.Shared
EntityFrameworkCore → Domain only
```

- Do **not** leak web concerns into Application/Domain layers.
- Do **not** call other application services within the same module. Push shared logic to Domain services or extract helpers.
- Controllers must depend on `Application.Contracts`, never on `Application` directly.

### Entity & Domain Conventions

- Entities use **rich domain model**: private/protected setters, behavior via methods.
- Include `protected` parameterless constructor for EF Core.
- Do **not** generate `Guid` keys inside constructors; accept `id` from `IGuidGenerator`.
- Reference other aggregate roots **by Id only**, not navigation properties.
- Domain services use `*Manager` suffix.
- Throw `BusinessException` with namespaced error codes for rule violations.

### Application Layer

- Interface naming: `I*AppService` inheriting `IApplicationService`.
- All methods `async`, end with `Async`.
- Accept/return **DTOs only**, never entities. Define DTOs in `*.Application.Contracts`.
- Make all public methods `virtual` for extensibility.
- This project uses **AutoMapper** (not Mapperly). Mapping profiles are `*AutoMapperProfile.cs` inheriting `Profile`.
- `ObjectMapper.Map<>()` is used for DTO mapping, not Mapperly partials.

### EF Core

- Entity configuration uses extension methods (`ConfigureMyProject()` on `ModelBuilder`), not inline in `OnModelCreating`.
- Always call `b.ConfigureByConvention()` for every entity mapping.
- Use `options.AddDefaultRepositories()` without `includeAllEntities: true`.
- Repository implementations inherit `EfCoreRepository<TDbContext, TEntity, TKey>`.

### Testing Conventions

- Test class naming: `*Tests.cs`
- Base class hierarchy: `AbpIntegratedTest<TModule>` → `GrantManagerTestBase<T>` → domain-specific bases.
- Use `[Fact]` for single tests, `[Theory]` with `[InlineData]` for parameterized.
- Assertions: `Shouldly` (`result.ShouldBe(expected)`, `result.ShouldNotBeNull()`).
- Mocking: `NSubstitute` (`Substitute.For<IService>()`).
- JSON test fixtures loaded from `AppDomain.CurrentDomain.BaseDirectory`.

### Code Style

- **Prettier** for JS/CSS: single quotes, 4-space tabs, no tabs.
- **C#**: Latest language version, nullable enabled in most projects.
- Localization: English strings in `*/Localization/*/en.json`. All user-facing text must use localization; no hardcoded English in code.
- Permissions defined in `*PermissionDefinitionProvider` in Application.Contracts.

### Branching

- `dev` → `test` → `main` promotion flow.
- Feature work: `feature/*`, bug fixes: `bugfix/*`, urgent fixes: `hotfix/*`.

## Skills

For detailed ABP patterns (DDD, application services, EF Core, testing, module architecture), refer to `.github/skills/` for domain-specific guidance.
