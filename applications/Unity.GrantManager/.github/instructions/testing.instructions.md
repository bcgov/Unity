---
applyTo: "**/*Tests*/**/*.cs,**/*Test*/**/*.cs"
description: "Testing standards using xUnit and Shouldly for ABP Framework"
---

# Testing Standards

Apply the repository-wide guidance from `../copilot-instructions.md` to all test code.

## Framework & Libraries

- Test framework: xUnit
- Assertion library: Shouldly (fluent assertions)
- Use `[Fact]` for single tests, `[Theory]` with `[InlineData]` for parameterized tests

## Test Class Conventions

- Suffix test classes with `_Tests` (e.g., `ApplicationAppService_Tests`)
- Test method naming: `Should_[Expected]_[Scenario]`
- Follow Arrange-Act-Assert pattern consistently
- Do not emit "Arrange", "Act", or "Assert" comments in generated tests

## ABP Test Base Classes

- Application service tests: Inherit `GrantManagerApplicationTestBase`
- Domain tests: Inherit `GrantManagerDomainTestBase`
- Web tests: Inherit `GrantManagerWebTestBase`

## Shouldly Assertions

- Use Shouldly fluent assertions exclusively — never use `Assert.*` methods
- `result.ShouldNotBeNull()` — existence checks
- `result.Title.ShouldBe("Expected")` — equality
- `list.ShouldContain(x => x.Id == id)` — collection membership
- `count.ShouldBeGreaterThan(0)` — numeric comparisons
- `await Should.ThrowAsync<BusinessException>(...)` — exception testing

## Multi-Tenancy Testing

- Test tenant data isolation using `CurrentTenant.Change(tenantId)`
- Verify that data created in one tenant is not visible in another
- Test both host-level and tenant-level operations

## Test Data Management

- Use helper methods for test data creation (e.g., `CreateTestApplicationAsync()`)
- Use static test data constants for well-known IDs
- Keep test data self-contained — each test should set up its own state

## TDD Workflow

- Write failing test first (Red)
- Implement minimal code to pass (Green)
- Refactor while keeping tests green (Refactor)
- Run full test suite after each change to catch regressions
