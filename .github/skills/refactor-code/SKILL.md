---
name: refactor-code
description: "Refactor code following ABP Framework best practices and DDD principles"
---

# Refactor Code

Refactor existing Unity Grant Manager code to improve quality, maintainability, and alignment with ABP Framework conventions and DDD principles.

Ask for the following if not provided:
- The code or files to refactor
- The refactoring goal (e.g., extract domain service, improve testability, fix layer violations)

## Requirements

- Preserve existing behavior — refactoring must not change functionality
- Ensure all existing tests pass after refactoring
- Follow ABP patterns: virtual methods, proper base classes, DTOs in application layer
- Respect layer boundaries — move logic to the correct architectural layer
- Extract business logic from application services into domain services (`*Manager`)
- Replace custom repositories with generic `IRepository<T, TKey>` when possible
- Improve nullable reference type annotations
- Simplify complex LINQ queries and improve readability
- Remove code duplication while maintaining ABP conventions

## Common Refactoring Patterns

- **Extract Domain Service**: Move business logic from AppService to Manager class
- **Introduce DTOs**: Replace entity exposure with proper DTO mapping
- **Fix Layer Violations**: Move code to correct architectural layer
- **Improve Testability**: Break dependencies, introduce interfaces
- **Multi-Tenancy Compliance**: Add `IMultiTenant`, fix DbContext usage
- **Modernize C#**: Apply C# 12 features where appropriate

## References

- [csharp.instructions.md](../../instructions/csharp.instructions.md) for C# standards
- [ARCHITECTURE.md](../../../ARCHITECTURE.md) for layer dependencies
