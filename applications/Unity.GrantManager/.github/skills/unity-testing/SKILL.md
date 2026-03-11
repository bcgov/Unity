---
name: unity-testing
description: Testing patterns for Unity - xUnit, Shouldly assertions, NSubstitute mocks, ABP test infrastructure. Use when writing or modifying unit tests or integration tests.
---

# Unity Testing Patterns

## Test Infrastructure

| Aspect | Value |
|--------|-------|
| Framework | xUnit 2.9.3 |
| Assertions | Shouldly 4.3.0 |
| Mocking | NSubstitute 5.3.0 |
| Database | SQLite in-memory (no PostgreSQL required) |
| Base Classes | ABP `AbpIntegratedTest<TModule>` |
| Target | .NET 9.0 |

## Test Project Locations

```
test/
  Unity.GrantManager.TestBase/          ← Shared fixtures & test data
  Unity.GrantManager.Application.Tests/ ← App service tests
  Unity.GrantManager.Domain.Tests/      ← Domain logic tests
  Unity.GrantManager.EntityFrameworkCore.Tests/
  Unity.GrantManager.Web.Tests/
modules/Unity.*/test/                   ← Each module has its own test projects
```

## Running Tests

```bash
# All tests (~470 tests, ~2 min)
dotnet test Unity.GrantManager.sln

# Single project
dotnet test test/Unity.GrantManager.Application.Tests/

# After build (faster)
dotnet test Unity.GrantManager.sln --no-build
```

## Base Class Hierarchy

```
AbpIntegratedTest<TStartupModule>           (Volo.Abp.Testing)
└── GrantManagerTestBase<TStartupModule>    (shared UoW helpers)
    ├── GrantManagerDomainTestBase           (domain tests)
    ├── GrantManagerEntityFrameworkCoreTestBase
    └── Module-specific bases:
        ├── FlexTestBaseModule
        ├── TenantManagementTestBase
        └── ReportingTestBase
```

## Writing Tests

### Unit Test Example (with mocking)

```csharp
public class MyServiceTests
{
    private readonly IMyRepository _repository;
    private readonly MyService _sut;

    public MyServiceTests()
    {
        _repository = Substitute.For<IMyRepository>();
        _sut = new MyService(_repository);
    }

    [Fact]
    public async Task CreateAsync_WithValidInput_ShouldSucceed()
    {
        // Arrange
        _repository.FindByNameAsync(Arg.Any<string>()).Returns((MyEntity?)null);

        // Act
        var result = await _sut.CreateAsync("test");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("test");
    }
}
```

### Integration Test Example (ABP)

```csharp
public class GrantAppServiceTests : GrantManagerApplicationTestBase
{
    private readonly IGrantAppService _grantAppService;

    public GrantAppServiceTests()
    {
        _grantAppService = GetRequiredService<IGrantAppService>();
    }

    [Fact]
    public async Task Should_Get_Grant_By_Id()
    {
        var result = await _grantAppService.GetAsync(GrantManagerTestData.GrantId);
        result.ShouldNotBeNull();
        result.Id.ShouldBe(GrantManagerTestData.GrantId);
    }
}
```

### Parameterized Tests

```csharp
[Theory]
[InlineData("schema1.json", 128)]
[InlineData("schema2.json", 10)]
public void TestMapping(string filename, int expectedCount)
{
    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", filename);
    var json = File.ReadAllText(path);
    var result = Parse(json);
    result.Count.ShouldBe(expectedCount);
}
```

## Test Data

- JSON fixtures are loaded from `AppDomain.CurrentDomain.BaseDirectory` subdirectories.
- Domain tests include JSON files in `Intake/Files/*.json` and `Intake/Mapping/*.json` (copied to output via `.csproj`).
- Shared test data constants live in `*TestData.cs` classes within TestBase projects.

## Web Tests

Web tests use `[Collection]` fixture pattern:

```csharp
[Collection(WebTestCollection.Name)]
public class MyWidgetTests
{
    private readonly IAbpLazyServiceProvider _lazyServiceProvider;

    public MyWidgetTests(WebTestFixture fixture)
    {
        _lazyServiceProvider = fixture.Services.GetRequiredService<IAbpLazyServiceProvider>();
    }
}
```

## Conventions

- Test class naming: `*Tests.cs`
- Method naming: `Should_ExpectedBehavior_When_Condition` or `MethodName_Scenario_ExpectedResult`
- Always use `Shouldly` for assertions (not `Assert.Equal`)
- Always use `NSubstitute` for mocking (not Moq)
- Test runner config: `xunit.runner.json` with `"shadowCopy": false`
