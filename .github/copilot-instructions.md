# Unity Portal – Copilot Instructions (Repository Root)

> This file is used by **Copilot on GitHub.com** (PR reviews, web chat). For local IDE instructions, see `applications/Unity.GrantManager/.github/copilot-instructions.md`.

## Project Summary

Unity is a **grant management portal** for the Province of British Columbia, built on **ABP Framework 10.3** with **.NET 10.0**, targeting **PostgreSQL 17**. The primary application code lives in `applications/Unity.GrantManager/`.

**Key stack:** .NET 10 · ABP 10.3 · EF Core 10.0 · PostgreSQL 17 · Redis · RabbitMQ · xUnit · Shouldly · NSubstitute · Mapperly · Cypress (E2E)

## Repository Structure

```
applications/Unity.GrantManager/   ← Main .NET solution (developers open this)
applications/Unity.AutoUI/        ← Cypress E2E tests (TypeScript)
database/scripts/                  ← SQL seed/migration scripts
documentation/                     ← Technical docs
.github/workflows/                 ← GitHub Actions CI/CD
```

## Key Conventions

- **ABP Framework** modular monolith with DDD layered architecture
- **Mapperly** for DTO mapping (not AutoMapper)
- **Razor Pages** UI with custom ABP theme (Unity.Theme.UX2)
- Multi-tenant architecture with separate host/tenant database contexts
- `dev` → `test` → `main` branch promotion flow
- PRs to `dev` from `feature/*`, `hotfix/*`, `bugfix/*`; PRs to `main` from `test` or `hotfix/*` only

## Build & Test (from `applications/Unity.GrantManager/`)

```bash
dotnet restore Unity.GrantManager.sln
dotnet build Unity.GrantManager.sln --no-restore
dotnet test Unity.GrantManager.sln --no-build
```

All PRs must pass `dotnet build` and `dotnet test` before merge. The CI runs all `*Tests.csproj` in a parallel matrix.

## Do NOT

- Use AutoMapper patterns — this project uses Mapperly
- Create repositories for child entities — only aggregate roots get repositories
- Put business logic in application services — use domain entities/services
