# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> This is **Unity Portal**, a grant management system for the Province of British Columbia. It is NOT the Unity game engine — do not suggest UnityEngine APIs.

## Repository Layout

`applications/Unity.GrantManager/` is where almost all work happens — a self-contained ABP Framework solution with its own extensive AI-agent instructions. `applications/Unity.AutoUI/` holds Cypress E2E tests; `applications/Unity.Tools/`, `database/`, and `documentation/` round out the rest (see root `README.md` for the full layout).

**Read these before making non-trivial changes in `applications/Unity.GrantManager/`** — note this solution has its own `.github/`, separate from the root `.github/` above it:

- `applications/Unity.GrantManager/.github/copilot-instructions.md` — authoritative project overview, layering, and conventions (trust this first)
- `applications/Unity.GrantManager/.github/instructions/*.instructions.md` — path-scoped rules for C#, EF Core, JavaScript, security, testing
- `applications/Unity.GrantManager/.github/skills/*/SKILL.md` — deep-dive patterns: DDD, application layer, EF Core, testing, ABP CLI, module structure
- `applications/Unity.GrantManager/.github/agents/*.agent.md` — planning agents for features, DDD modeling, EF migrations, permissions/localization audits, test strategy, PR readiness

Where those files and this one overlap, prefer the more specific ones under `applications/Unity.GrantManager/.github/`.

## Documentation

`documentation/` holds prose docs for four areas: Flex, Tenant Management (incl. Onboarding), Reporting, and the Applicant Portal integration. **`documentation/README.md` is a source-path → doc index** — use it to find out whether the code you touched is documented.

Before finishing a change, look up the paths you edited in that index and fix anything your change made **false** (a renamed class, a changed state machine or ordered sequence, a removed endpoint, a roadmap item you actually fixed). The bar is *"is anything here now wrong?"* — not *"should I write up what I did?"*. Most changes need no doc edit; doc updates that are needed go in the same commit as the code.

Do not create documentation for areas that have none — `documentation/README.md` lists the deliberately undocumented modules, and filling those gaps is its own ticket. Full guidance is in `applications/Unity.GrantManager/.claude/rules/documentation.md`, which loads automatically when you edit a documented area.

## Build & Test

All commands run from `applications/Unity.GrantManager/`:

```bash
dotnet restore Unity.GrantManager.sln
dotnet build Unity.GrantManager.sln --no-restore      # ~3 min, 81 projects
dotnet test Unity.GrantManager.sln --no-build          # ~470 tests, ~1-2 min

# Single test project
dotnet test test/Unity.GrantManager.Application.Tests/ --no-build
```

- No PostgreSQL setup needed for tests — SQLite in-memory (most projects) or `EFCore.InMemory` (`Unity.GrantManager.Web.Tests`).
- `Unity.GrantManager.Web/Pages/Dashboard/Index.cshtml.cs` has one expected `CS8604` warning — don't fix it unless asked.
- `Directory.Build.props` / `common.props` (repo-wide MSBuild props) already suppress `NU1701`, `MSB3277`, `CS1591` — don't re-suppress per-project.

### Local dev environment

`docker-compose.yml` + `.env.example` in `applications/Unity.GrantManager/` spin up the web app, PostgreSQL, a DB migrator, and Redis. Copy `.env.example` to `.env` and fill in secrets before running `docker compose up`.

### EF Core migrations

There are **two separate database contexts** — always specify which one:

```bash
cd applications/Unity.GrantManager/src/Unity.GrantManager.EntityFrameworkCore

dotnet ef migrations add <Name> --context GrantManagerDbContext --output-dir Migrations/HostMigrations   # host/shared tables
dotnet ef migrations add <Name> --context GrantTenantDbContext --output-dir Migrations/TenantMigrations   # per-tenant data
```

## Architecture

ABP Framework modular monolith, DDD-layered — see `applications/Unity.GrantManager/.github/copilot-instructions.md` and `applications/Unity.GrantManager/.github/skills/unity-module-structure/SKILL.md` for the full module list and dependency-direction diagram.

Business rules belong in Domain entities/managers, not controllers or app services. Don't call another app service within the same module — push shared logic into a domain service.

### Key conventions

C#, EF Core, JavaScript, security, and testing conventions are detailed in `.claude/rules/*.md` (loaded automatically for matching files) and `applications/Unity.GrantManager/.github/instructions/*.instructions.md`.

- **Branching**: `dev` → `test` → `main` promotion. Feature branches `feature/*`, fixes `bugfix/*`, urgent `hotfix/*`. PRs to `dev` come from `feature/*`/`bugfix/*`/`hotfix/*`; PRs to `main` only from `test` or `hotfix/*`.
- **Commit messages**: prefix with `[AB#<ID>]` extracted from the branch name (e.g. `feature/AB#32037-...` → `AB#32037`), then a short description.
