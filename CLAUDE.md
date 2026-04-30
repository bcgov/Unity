# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity Grant Manager is a BC Government multi-tenant grant management and adjudication web application. It is built on **ABP Framework v9.1.3** (.NET 9.0), deployed on **Red Hat OpenShift**, and targets a PostgreSQL database.

The primary application lives at `applications/Unity.GrantManager/` with its own Visual Studio solution (`Unity.GrantManager.sln`).

---

## Common Commands

All commands should be run from `applications/Unity.GrantManager/` unless otherwise noted.

### Build

```bash
dotnet restore Unity.GrantManager.sln
dotnet build Unity.GrantManager.sln
```

### Run Tests

Run the full test suite:
```bash
dotnet test Unity.GrantManager.sln
```

Run a specific test project:
```bash
dotnet test test/Unity.GrantManager.Application.Tests/Unity.GrantManager.Application.Tests.csproj
dotnet test modules/Unity.Flex/test/Unity.Flex.Application.Tests/Unity.Flex.Application.Tests.csproj
```

Run a single test method:
```bash
dotnet test test/Unity.GrantManager.Application.Tests/ --filter "FullyQualifiedName~MyTestMethodName"
```

### Local Development Stack (Docker Compose)

```bash
# Start full local stack (PostgreSQL, Redis, RabbitMQ, Keycloak, Nginx, app)
docker compose up

# Rebuild and start
docker compose up --build

# Run DB migrations only
docker compose run unity-data-dbmigrator
```

The web app is proxied via Nginx at `http://localhost:42080`.

### Frontend Assets

Frontend dependencies are managed via Yarn and the ABP CLI:
```bash
cd src/Unity.GrantManager.Web
abp install-libs
```

### Docker Image Builds

```bash
docker build -t unity-grantmanager-web -f src/Unity.GrantManager.Web/Dockerfile .
docker build -t unity-grantmanager-dbmigrator -f src/Unity.GrantManager.DbMigrator/Dockerfile .
```

---

## Architecture

### Technology Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 9.0 / ASP.NET Core |
| App Framework | ABP Framework (Volo.Abp) v9.1.3 |
| Web UI | ASP.NET Core MVC + Razor Pages |
| ORM | Entity Framework Core 9 + PostgreSQL (Npgsql) |
| Auth | OpenID Connect via Keycloak (BC Gov SSO) |
| Background Jobs | Quartz.NET (clustered) |
| Caching | Redis (StackExchange.Redis) |
| Message Bus | RabbitMQ (ABP EventBus) |
| Blob Storage | AWS S3 |
| Logging | Serilog |
| IoC | Autofac |
| Frontend | Bootstrap 5, DataTables, ECharts, TinyMCE, Form.io |

### ABP Layered Architecture

The main application follows ABP's standard layered structure under `src/`:

```
Unity.GrantManager.Domain.Shared/         - Enums, constants, shared DTOs
Unity.GrantManager.Domain/                - Entities, aggregates, domain services
Unity.GrantManager.Application.Contracts/ - Service interfaces, DTOs
Unity.GrantManager.Application/           - Application service implementations
Unity.GrantManager.EntityFrameworkCore/   - EF Core DbContext, repositories, migrations
Unity.GrantManager.HttpApi/               - REST API controllers
Unity.GrantManager.HttpApi.Client/        - HTTP client proxy
Unity.GrantManager.Web/                   - MVC web entry point, Pages, ViewComponents
Unity.GrantManager.DbMigrator/            - Database migration runner (CLI)
```

### Modules

Reusable domain modules live under `modules/`. Each follows the same ABP layered pattern (Domain, Application, Web, EF, Tests):

| Module | Purpose |
|---|---|
| `Unity.SharedKernel` | Shared base types, RabbitMQ event bus config |
| `Unity.Identity.Web` | Keycloak / BC Gov SSO integration |
| `Unity.TenantManagement` | Multi-tenant management |
| `Unity.Flex` | Dynamic/flexible forms engine |
| `Unity.Notifications` | Email via CHES (Common Hosted Email Service) |
| `Unity.Payments` | BC Gov CAS (Corporate Accounting System) payments |
| `Unity.Reporting` | Reporting module |
| `Unity.AI` | AI-powered analysis (Azure OpenAI integration) |
| `Unity.Theme.UX2` | Custom BC Gov UX2 theme for ABP |

All modules are wired into the main app via `[DependsOn(...)]` in `GrantManagerWebModule`.

### Multi-Tenancy

The application is ABP multi-tenant. Host and tenant databases use separate connection strings (`Default` for host, `Tenant` for tenants). Tenant resolution is handled by ABP's built-in tenant middleware.

### External Integrations

- **Keycloak** (`dev.loginproxy.gov.bc.ca`) — BC Gov SSO proxy for OIDC auth
- **CSS API** — BC Government Common Shared Services for user role lookups
- **CAS API** — Corporate Accounting System for payment processing
- **CHES** — Common Hosted Email Service for notifications
- **CHEFS / Intake Forms** — BC Gov form submission platform (API key auth)
- **AWS S3** — Blob/document storage
- **BC Geocoder** — Geographic lookups (electoral districts, economic regions)

---

## Branch and CI/CD Flow

```
feature/* / bugfix/* / hotfix/*
        |
        v (PR)
       dev   --> builds Docker images --> deploys to OpenShift dev namespace
        |
        v (PR)
      test   --> builds Docker images --> deploys to OpenShift test namespace
        |
        v (PR)
      main   --> builds + tags stable images --> deploys to OpenShift prod
```

PRs run all unit tests in a parallel matrix (GitHub Actions). Results are posted as PR comments. A Microsoft Teams notification is sent on test results for `main`-targeting PRs.

Images are pushed to **JFrog Artifactory** and deployed to **Red Hat OpenShift**.

---

## Test Infrastructure

- **Framework:** xUnit v2 with Shouldly assertions and NSubstitute mocking
- **Base classes:** `Volo.Abp.TestBase` — all test projects reference `Unity.GrantManager.TestBase` (or module-specific `TestBase`)
- **E2E tests:** Cypress (TypeScript) in `applications/Unity.AutoUI/` — targets the live web UI

Shared MSBuild properties for all projects are in `applications/Unity.GrantManager/common.props`.

---

## Commit Message Format

```
AB#<ID> - short description
```

Extract `<ID>` from the branch name (e.g., `feature/AB#32037-...` → `AB#32037`). Aim for ~50 characters after the prefix.

---

## Code Conventions (Critical Rules)

### C# / ABP
- **Never** use `Guid.NewGuid()` → use `GuidGenerator.Create()`
- **Never** use `DateTime.Now` / `DateTime.UtcNow` → use `Clock.Now`
- **Never** use `services.AddScoped<>()` for ABP services → use `ITransientDependency`, `ISingletonDependency`, or `IScopedDependency`
- **Never** call other app services within the same module → extract shared logic to domain services
- App service method names should NOT embed the entity name: use `GetAsync`, not `GetApplicationAsync`
- All user-facing strings must use localization (`L["Key"]`). No hardcoded English in code.
- Use `BusinessException` with namespaced error codes (e.g., `"GrantManager:ApplicationNotFound"`)
- All public app service methods must be `virtual`
- Use **AutoMapper** for all DTO mapping — **not** Mapperly

### EF Core Migrations

Always run from `src/Unity.GrantManager.EntityFrameworkCore` and specify context:

```bash
# Host migrations
dotnet ef migrations add <Name> --context GrantManagerDbContext --output-dir Migrations/HostMigrations

# Tenant migrations
dotnet ef migrations add <Name> --context GrantTenantDbContext --output-dir Migrations/TenantMigrations
```

Do **not** use `includeAllEntities: true` with `AddDefaultRepositories()`.

### Testing
- Assertions: **Shouldly** only — never `Assert.*`
- Mocking: **NSubstitute** only — never Moq
- Test method naming: `Should_ExpectedBehavior_When_Condition`
- ~580 tests, ~1–2 min run time. Use `--no-build` after a fresh build.
- Tests use SQLite in-memory (no PostgreSQL needed)
- There is **1 expected build warning** (CS8604 in `Dashboard/Index.cshtml.cs`) — do not try to fix it

---

## Workflow Agents

Agent definitions live at `applications/Unity.GrantManager/.github/agents/`. Use them to accelerate ABP-aligned work:

| Agent | When to Use |
|---|---|
| `feature-planner` | Convert feature/bug request into layer-by-layer implementation plan |
| `ddd-modeler` | Design aggregates, invariants, repositories, domain managers |
| `application-service-designer` | Define DTOs, service contracts, AutoMapper changes |
| `efcore-migration-planner` | Plan host vs. tenant EF Core schema changes and migrations |
| `permissions-localization-auditor` | Audit diff for missing permissions and hardcoded strings |
| `test-strategy` | Generate risk-based unit/integration test plans |
| `test-triage` | Diagnose failing tests and propose minimal fixes |
| `pr-readiness` | Final pre-PR quality gate (layering, mapping, CI) |
| `pre-readiness-deep` | Deep pre-PR review (extended quality gate) |

Skills (detailed patterns) live at `applications/Unity.GrantManager/.github/skills/`.
