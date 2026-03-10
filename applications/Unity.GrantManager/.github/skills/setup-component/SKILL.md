---
name: setup-component
description: "Scaffold a new ABP Framework component following DDD layered architecture"
---

# Setup Component

Create a new ABP Framework component (entity, application service, API, and UI) following the Unity Grant Manager's DDD layered architecture and ABP conventions.

Ask for the following if not provided:
- Component name (e.g., "Assessment", "PaymentRequest")
- Whether it is tenant-scoped or host-scoped
- Required properties and their types
- Whether it needs a domain service (complex business logic)

## Requirements

- Follow ABP's layered architecture: Domain → Application → HttpApi → Web
- Use the existing project structure and naming conventions
- Inherit from proper ABP base classes (`FullAuditedAggregateRoot`, `ApplicationService`, `AbpController`)
- All public methods must be `virtual`
- Application services return DTOs only, never entities
- Implement `IMultiTenant` for tenant-scoped entities
- Apply `[Authorize]` attributes with permission names
- Configure entity in the correct DbContext (`GrantManagerDbContext` or `GrantTenantDbContext`)
- Add constants to Domain.Shared project
- Create AutoMapper profile in Application project
- Generate corresponding xUnit test class with Shouldly assertions
- Add localization keys to resource files

## Layer Checklist

1. **Domain.Shared**: Constants, enums
2. **Domain**: Entity, repository interface (if custom), domain service (if needed)
3. **Application.Contracts**: DTOs, service interface
4. **Application**: Service implementation, AutoMapper profile
5. **EntityFrameworkCore**: Entity configuration, DbSet, migration
6. **HttpApi**: API controller
7. **Web**: Razor Pages, JavaScript
8. **Tests**: Application service tests

## References

- [ARCHITECTURE.md](../../../ARCHITECTURE.md) for layer dependencies
- [CONTRIBUTING.md](../../../CONTRIBUTING.md) for coding conventions
- [copilot-instructions.md](../../copilot-instructions.md) for ABP patterns
