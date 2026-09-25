# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> This is **Unity Portal**, a grant management system for the Province of British Columbia. It is NOT the Unity game engine — do not suggest UnityEngine APIs.

## Repository Layout

`applications/Unity.GrantManager/` is where almost all work happens — a self-contained ABP Framework solution with its own extensive AI-agent instructions. `applications/Unity.GrantManager.Angular/` is the Angular front end for the in-progress strangler-fig UI migration (see [`documentation/angular-strangler-fig/architecture.md`](documentation/angular-strangler-fig/architecture.md) and that app's `README.md` for local setup); `applications/Unity.AutoUI/` holds Cypress E2E tests; `applications/Unity.Tools/`, `database/`, and `documentation/` round out the rest (see root `README.md` for the full layout).

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

## Unity.GrantManager specifics

Build/test commands, EF Core migration commands, and architecture/layering conventions for `applications/Unity.GrantManager/` live in `applications/Unity.GrantManager/CLAUDE.md`, which loads automatically whenever you work under that directory.

## Key conventions

- **Branching**: `dev` → `test` → `main` promotion. Feature branches `feature/*`, fixes `bugfix/*`, urgent `hotfix/*`. PRs to `dev` come from `feature/*`/`bugfix/*`/`hotfix/*`; PRs to `main` only from `test` or `hotfix/*`.
- **Commit messages**: prefix with `[AB#<ID>]` extracted from the branch name (e.g. `feature/AB#32037-...` → `AB#32037`), then a short description.
