---
name: write-tests
description: "Generate xUnit tests with Shouldly assertions following ABP testing conventions"
---

# Write Tests

Generate comprehensive test suites for Unity Grant Manager components using xUnit and Shouldly, following the project's TDD conventions and ABP testing patterns.

Ask for the following if not provided:
- The class or feature to test
- Test scope (unit, integration, or both)
- Specific scenarios or edge cases to cover

## Requirements

- Use xUnit with `[Fact]` and `[Theory]` attributes
- Use Shouldly for all assertions — never use `Assert.*` methods
- Follow `Should_[Expected]_[Scenario]` naming convention
- Inherit from the correct ABP test base class:
  - `GrantManagerApplicationTestBase` for application service tests
  - `GrantManagerDomainTestBase` for domain logic tests
  - `GrantManagerWebTestBase` for web layer tests
- Follow Arrange-Act-Assert pattern without section comments
- Test multi-tenancy isolation when entities implement `IMultiTenant`
- Test authorization by verifying permission enforcement
- Use helper methods for test data creation
- Test both happy paths and error scenarios
- Verify persistence by reading back from repository after mutations

## Test Categories

- **Entity tests**: Constructor validation, business methods, state transitions
- **Domain service tests**: Business rule enforcement, validation logic
- **Application service tests**: CRUD operations, DTO mapping, authorization
- **Integration tests**: End-to-end workflows, multi-tenancy isolation

## References

- [testing.instructions.md](../../instructions/testing.instructions.md) for testing standards
- [copilot-instructions.md](../../copilot-instructions.md) for ABP test patterns
