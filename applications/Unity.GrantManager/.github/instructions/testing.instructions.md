---
applyTo: "**/test/**/*.cs"
---

# Testing Conventions for Unity Grant Manager

- Framework: **xUnit 2.9.3** with **Shouldly 4.3.0** assertions and **NSubstitute 5.3.0** mocks.
- Tests use in-memory database providers (SQLite in-memory for most test projects; `Unity.GrantManager.Web.Tests` uses `Microsoft.EntityFrameworkCore.InMemory`). No external PostgreSQL/database setup is required.
- Test class naming: `*Tests.cs`.
- Base class hierarchy: `AbpIntegratedTest<TModule>` → `GrantManagerTestBase<T>` → domain-specific bases.
- Use `[Fact]` for single tests, `[Theory]` with `[InlineData]` for parameterized.
- Assertions: Shouldly (`result.ShouldBe(expected)`, `result.ShouldNotBeNull()`). Do NOT use `Assert.*`.
- Mocking: NSubstitute (`Substitute.For<IService>()`). Do NOT use Moq.
- JSON test fixtures loaded from `AppDomain.CurrentDomain.BaseDirectory`.
- Run all tests: `dotnet test Unity.GrantManager.sln --no-build`
- Test method naming: `Should_[Expected]_[Scenario]`
- Follow Arrange-Act-Assert pattern consistently
- Do not emit "Arrange", "Act", or "Assert" comments in generated tests

## Multi-Tenancy Testing

- Test tenant data isolation using `CurrentTenant.Change(tenantId)`
- Verify that data created in one tenant is not visible in another
- Test both host-level and tenant-level operations

## Test Data Management

- Use helper methods for test data creation (e.g., `CreateTestApplicationAsync()`)
- Use static test data constants for well-known IDs
- Keep test data self-contained — each test should set up its own state
