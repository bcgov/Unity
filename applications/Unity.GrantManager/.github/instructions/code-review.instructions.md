---
applyTo: "**/*"
description: "Code review standards for Unity Grant Manager"
---

# Code Review Standards

Apply the repository-wide guidance from `../copilot-instructions.md` when reviewing code.

## ABP Framework Compliance

- Verify correct use of ABP base classes and inheritance
- Ensure all public methods are `virtual` for ABP extensibility
- Confirm application services return DTOs, never entities
- Check domain services use `Manager` suffix and contain business logic
- Verify `[Authorize]` attributes are applied with correct permission names

## Architecture & Layer Boundaries

- Ensure strict layer dependency direction (Domain ← Application ← Web)
- Verify Domain layer has no dependencies on Application or Infrastructure
- Confirm Application.Contracts depends only on Domain.Shared
- Check that EF Core layer depends only on Domain

## Multi-Tenancy

- Verify tenant-scoped entities implement `IMultiTenant`
- Confirm correct DbContext usage (host vs tenant)
- Ensure no manual TenantId filtering
- Check for cross-tenant data leaks

## Code Quality

- Nullable reference types handled correctly — no suppression without justification
- Async/await used consistently with `Async` suffix on method names
- Error handling uses `BusinessException` with meaningful error codes
- No hardcoded strings — use localization and constants

## Testing

- Verify tests follow `Should_[Expected]_[Scenario]` naming
- Check that Shouldly assertions are used, not `Assert.*`
- Ensure critical paths have test coverage
- Verify multi-tenancy isolation tests for tenant-scoped features

## Security

- No secrets or connection strings in code
- Input validation at application service boundary
- Authorization checks present on all mutating operations
- No raw SQL with string concatenation

## Frontend

- JavaScript wrapped in IIFE pattern
- ABP localization used for all user-facing strings
- ABP dynamic proxies used instead of manual AJAX
- DataTable reload called after CRUD operations
- Modal Manager used for dialog management
