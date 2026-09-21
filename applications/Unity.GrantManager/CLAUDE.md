# CLAUDE.md (Unity.GrantManager)

This file loads automatically when working under `applications/Unity.GrantManager/`. See the root `CLAUDE.md` for repository-wide conventions (branching, commit messages, documentation policy).

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
