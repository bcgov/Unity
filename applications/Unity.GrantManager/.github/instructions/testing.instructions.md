---
applyTo: "**/test/**/*.cs"
---

# Testing Conventions for Unity Grant Manager

- Framework: **xUnit 2.9.3** with **Shouldly 4.3.0** assertions and **NSubstitute 5.3.0** mocks.
- Tests use **SQLite in-memory** databases. No external database setup required.
- Test class naming: `*Tests.cs`.
- Base class hierarchy: `AbpIntegratedTest<TModule>` → `GrantManagerTestBase<T>` → domain-specific bases.
- Use `[Fact]` for single tests, `[Theory]` with `[InlineData]` for parameterized.
- Assertions: Shouldly (`result.ShouldBe(expected)`, `result.ShouldNotBeNull()`). Do NOT use `Assert.*`.
- Mocking: NSubstitute (`Substitute.For<IService>()`). Do NOT use Moq.
- JSON test fixtures loaded from `AppDomain.CurrentDomain.BaseDirectory`.
- Run all tests: `dotnet test Unity.GrantManager.sln --no-build`
