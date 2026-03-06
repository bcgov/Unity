# Contributing to Unity Grant Manager

## Overview

Unity Grant Manager is built on **ABP Framework 9.1.3** following Domain-Driven Design (DDD) principles. This guide outlines coding conventions, patterns, and best practices to ensure consistency and maintainability across the codebase.

## Prerequisites

- .NET 9.0 SDK
- PostgreSQL 15 or higher
- ABP CLI (`dotnet tool install -g Volo.Abp.Cli`)
- Visual Studio 2022 or JetBrains Rider
- Docker Desktop (for local Redis and RabbitMQ)

## Getting Started

1. **Clone the repository** and navigate to `Unity.GrantManager`
2. **Install JavaScript dependencies**: Run `abp install-libs` in the application root
3. **Apply database migrations**: Set `Unity.GrantManager.DbMigrator` as startup project and run (Ctrl+F5)
4. **Run the application**: Set `Unity.GrantManager.Web` as startup project and run (F5)

## ABP Framework Conventions

Unity Grant Manager follows ABP Framework's opinionated architecture and best practices. All contributors should familiarize themselves with:

- [ABP Best Practices Guide](https://docs.abp.io/en/abp/latest/Best-Practices)
- [Module Architecture Best Practices](https://docs.abp.io/en/abp/latest/Best-Practices/Module-Architecture)
- [Implementing Domain Driven Design (e-book)](https://abp.io/books/implementing-domain-driven-design)

## Project Structure & Layers

Unity Grant Manager follows ABP's layered architecture with strict dependency rules:

```
Unity.GrantManager.Domain.Shared     (Constants, Enums)
  ↑
Unity.GrantManager.Domain            (Entities, Domain Services, Repository Interfaces)
  ↑
Unity.GrantManager.Application.Contracts  (Service Interfaces, DTOs)
  ↑
Unity.GrantManager.Application       (Application Services Implementation)
  ↑
Unity.GrantManager.EntityFrameworkCore    (DbContext, Repositories, EF Configuration)
  ↑
Unity.GrantManager.HttpApi           (API Controllers)
  ↑
Unity.GrantManager.Web               (Razor Pages, UI Components)
```

**Dependency Rules:**
- Domain layer has no dependencies on other layers (only ABP framework)
- Application.Contracts depends only on Domain.Shared
- Application depends on Domain and Application.Contracts
- EntityFrameworkCore depends only on Domain
- Higher layers can depend on lower layers, but not vice versa

## Coding Conventions

### C# Language Features

- **Target Framework**: .NET 9.0
- **Language Version**: C# 12.0 (latest)
- **Nullable Reference Types**: Enabled project-wide
  - Always declare nullability explicitly: `string?` for nullable, `string` for non-nullable
  - Use `null!` only when you're certain a value won't be null (e.g., dependency injection)
  - Avoid nullable warnings; fix them properly

### Naming Conventions

- **Classes, Methods, Properties**: PascalCase (e.g., `GrantApplication`, `CreateApplicationAsync`)
- **Private Fields**: Camel case with underscore prefix (e.g., `_repository`, `_logger`)
- **Parameters, Local Variables**: Camel case (e.g., `applicationDto`, `userId`)
- **Constants**: PascalCase (e.g., `MaxApplicationTitleLength`)
- **Interfaces**: PascalCase with `I` prefix (e.g., `IApplicationRepository`)
- **Domain Services**: Suffix with `Manager` (e.g., `ApplicationManager`, `AssessmentManager`)
- **Application Services**: Suffix with `AppService` (e.g., `ApplicationAppService`)
- **DTOs**: Suffix with purpose (e.g., `CreateApplicationDto`, `ApplicationDto`)

### Code Style

- **Indentation**: 4 spaces (no tabs)
- **Line Length**: Aim for 120 characters max
- **Braces**: Always use braces for control structures, even single-line statements
- **Access Modifiers**: Always specify explicitly (e.g., `public`, `private`, `protected`)
- **Using Directives**: Place at the top of the file, outside namespace
- **Async Suffix**: Always suffix async methods with `Async` (e.g., `CreateAsync`, `GetListAsync`)

## Domain Layer Patterns

### Entities & Aggregate Roots

**Inherit from ABP base classes:**

```csharp
// For entities with GUID keys
public class GrantApplication : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }  // Required for multi-tenant entities
    
    // Properties with private setters for encapsulation
    public string Title { get; private set; } = string.Empty;
    public ApplicationStatus Status { get; private set; }
    
    // Parameterless constructor for EF Core
    private GrantApplication() { }
    
    // Public constructor with required parameters
    public GrantApplication(Guid id, string title) : base(id)
    {
        SetTitle(title);
        Status = ApplicationStatus.Draft;
    }
    
    // Business logic methods (not simple setters)
    public void SetTitle(string title)
    {
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), MaxTitleLength);
    }
    
    public void Submit()
    {
        if (Status != ApplicationStatus.Draft)
            throw new BusinessException("Application can only be submitted from Draft status");
            
        Status = ApplicationStatus.Submitted;
        
        // Publish domain event
        AddDistributedEvent(new ApplicationSubmittedEto { ApplicationId = Id, Title = Title });
    }
}
```

**Best Practices:**
- Use `FullAuditedAggregateRoot<TKey>` for entities requiring full audit trails (creation, modification, deletion tracking)
- Use `AuditedAggregateRoot<TKey>` if soft-delete is not needed
- Use `AggregateRoot<TKey>` for simple entities without auditing
- Implement `IMultiTenant` for tenant-specific entities (stored in `GrantTenantDbContext`)
- Use private setters and expose business methods instead
- Validate input in constructors and methods using `Check` helper class
- Publish domain events using `AddLocalEvent()` or `AddDistributedEvent()`

### Domain Services

**Use the `Manager` suffix and inherit from `DomainService`:**

```csharp
public class ApplicationManager : DomainService
{
    private readonly IRepository<GrantApplication, Guid> _applicationRepository;
    private readonly IRepository<GrantProgram, Guid> _programRepository;
    
    public ApplicationManager(
        IRepository<GrantApplication, Guid> applicationRepository,
        IRepository<GrantProgram, Guid> programRepository)
    {
        _applicationRepository = applicationRepository;
        _programRepository = programRepository;
    }
    
    public virtual async Task<GrantApplication> CreateAsync(
        string title,
        Guid programId,
        Guid applicantId)
    {
        // Validate business rules
        var program = await _programRepository.GetAsync(programId);
        if (!program.IsAcceptingApplications())
            throw new BusinessException("Program is not currently accepting applications");
        
        // Create entity
        var application = new GrantApplication(GuidGenerator.Create(), title);
        application.SetProgram(programId);
        application.SetApplicant(applicantId);
        
        return await _applicationRepository.InsertAsync(application);
    }
    
    public virtual async Task ValidateForSubmission(GrantApplication application)
    {
        // Complex validation logic that doesn't belong in the entity
        if (string.IsNullOrWhiteSpace(application.Title))
            throw new BusinessException("Application must have a title");
            
        // Check for duplicate submissions
        var existingCount = await _applicationRepository.CountAsync(x => 
            x.ApplicantId == application.ApplicantId && 
            x.ProgramId == application.ProgramId &&
            x.Status != ApplicationStatus.Draft);
            
        if (existingCount > 0)
            throw new BusinessException("You have already submitted an application for this program");
    }
}
```

**Best Practices:**
- Use domain services for business logic that doesn't naturally fit within a single entity
- Only include state-changing methods; use repositories directly for queries in application services
- Make methods `virtual` to allow overriding in derived classes
- Throw `BusinessException` with clear error codes for domain validation failures
- Accept and return domain entities only (never DTOs)
- Do not implement interfaces unless there's a specific need for multiple implementations

### Repositories

**Define custom repository interfaces only when needed:**

```csharp
public interface IApplicationRepository : IRepository<GrantApplication, Guid>
{
    Task<List<GrantApplication>> GetApplicationsByProgramAsync(Guid programId, CancellationToken cancellationToken = default);
    
    Task<int> GetSubmittedCountByApplicantAsync(Guid applicantId, CancellationToken cancellationToken = default);
}
```

**Best Practices:**
- Use `IRepository<TEntity, TKey>` generic repository for standard CRUD operations
- Define custom repository interface only for complex queries or specialized operations
- Place repository interfaces in the Domain project
- Implement custom repositories in EntityFrameworkCore project
- Use async methods with `CancellationToken` support
- Return domain entities, not DTOs (mapping happens in application layer)

## Application Layer Patterns

### Application Services

**Inherit from `ApplicationService` and implement interface from Application.Contracts:**

```csharp
// In Application.Contracts project
public interface IApplicationAppService : IApplicationService
{
    Task<ApplicationDto> GetAsync(Guid id);
    Task<PagedResultDto<ApplicationDto>> GetListAsync(GetApplicationListDto input);
    Task<ApplicationDto> CreateAsync(CreateApplicationDto input);
    Task<ApplicationDto> UpdateAsync(Guid id, UpdateApplicationDto input);
    Task DeleteAsync(Guid id);
}

// In Application project
public class ApplicationAppService : ApplicationService, IApplicationAppService
{
    private readonly IRepository<GrantApplication, Guid> _applicationRepository;
    private readonly ApplicationManager _applicationManager;
    
    public ApplicationAppService(
        IRepository<GrantApplication, Guid> applicationRepository,
        ApplicationManager applicationManager)
    {
        _applicationRepository = applicationRepository;
        _applicationManager = applicationManager;
    }
    
    [Authorize(GrantManagementPermissions.Applications.Create)]
    public virtual async Task<ApplicationDto> CreateAsync(CreateApplicationDto input)
    {
        // Use domain service for business logic
        var application = await _applicationManager.CreateAsync(
            input.Title,
            input.ProgramId,
            CurrentUser.GetId());
        
        await _applicationRepository.InsertAsync(application);
        
        return ObjectMapper.Map<GrantApplication, ApplicationDto>(application);
    }
    
    [Authorize(GrantManagementPermissions.Applications.Default)]
    public virtual async Task<ApplicationDto> GetAsync(Guid id)
    {
        var application = await _applicationRepository.GetAsync(id);
        return ObjectMapper.Map<GrantApplication, ApplicationDto>(application);
    }
    
    [Authorize(GrantManagementPermissions.Applications.Default)]
    public virtual async Task<PagedResultDto<ApplicationDto>> GetListAsync(GetApplicationListDto input)
    {
        var queryable = await _applicationRepository.GetQueryableAsync();
        
        // Apply filters
        queryable = queryable.WhereIf(!input.Filter.IsNullOrWhiteSpace(), 
            x => x.Title.Contains(input.Filter!));
        
        // Get total count
        var totalCount = await AsyncExecuter.CountAsync(queryable);
        
        // Apply sorting and paging
        queryable = queryable
            .OrderBy(input.Sorting ?? "Title")
            .PageBy(input.SkipCount, input.MaxResultCount);
        
        // Execute query and map to DTOs
        var applications = await AsyncExecuter.ToListAsync(queryable);
        var dtos = ObjectMapper.Map<List<GrantApplication>, List<ApplicationDto>>(applications);
        
        return new PagedResultDto<ApplicationDto>(totalCount, dtos);
    }
}
```

**Best Practices:**
- One application service per aggregate root
- Make all public methods `virtual` for extensibility
- Use `[Authorize]` attributes for permission checks
- Accept and return DTOs only (never expose domain entities directly)
- Use `ObjectMapper` for entity-to-DTO mapping (AutoMapper)
- Use `CurrentUser` to access current user information
- Use `AsyncExecuter` to execute async LINQ queries
- Methods are automatically wrapped in Unit of Work (transaction)
- Use domain services for complex business logic
- Use `WhereIf`, `OrderBy`, `PageBy` extension methods for querying

### Data Transfer Objects (DTOs)

**Define DTOs in Application.Contracts project:**

```csharp
// Input DTO
public class CreateApplicationDto
{
    [Required]
    [StringLength(ApplicationConsts.MaxTitleLength)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public Guid ProgramId { get; set; }
    
    public string? Description { get; set; }
}

// Output DTO
public class ApplicationDto : AuditedEntityDto<Guid>
{
    public string Title { get; set; } = string.Empty;
    public Guid ProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; }
    public string? Description { get; set; }
}

// List query DTO
public class GetApplicationListDto : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public ApplicationStatus? Status { get; set; }
}
```

**Best Practices:**
- Use data annotations for validation (`[Required]`, `[StringLength]`, etc.)
- Inherit from ABP base DTO classes when appropriate:
  - `EntityDto<TKey>`: Basic DTO with ID
  - `AuditedEntityDto<TKey>`: Includes creation time and creator
  - `FullAuditedEntityDto<TKey>`: Includes modification and deletion info
  - `PagedAndSortedResultRequestDto`: For list queries with paging/sorting
- Use nullable types (`?`) for optional properties
- Initialize string properties to `string.Empty` to avoid nullable warnings
- Define constants for string lengths in Domain.Shared project

### Object Mapping (AutoMapper)

**Configure mappings in Application project's module class:**

```csharp
public class GrantManagerApplicationAutoMapperProfile : Profile
{
    public GrantManagerApplicationAutoMapperProfile()
    {
        // Entity to DTO (read)
        CreateMap<GrantApplication, ApplicationDto>();
        
        // DTO to Entity (write) - rarely used, prefer constructors
        CreateMap<CreateApplicationDto, GrantApplication>()
            .Ignore(x => x.Id)
            .Ignore(x => x.TenantId);
    }
}
```

## Entity Framework Core Patterns

### DbContext Configuration

**Two DbContext classes for multi-tenancy:**

```csharp
// Host database context (non-tenant data)
[ConnectionStringName("Default")]
public class GrantManagerDbContext : AbpDbContext<GrantManagerDbContext>
{
    public DbSet<IdentityUser> Users { get; set; } = null!;
    public DbSet<Tenant> Tenants { get; set; } = null!;
    
    public GrantManagerDbContext(DbContextOptions<GrantManagerDbContext> options) 
        : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ConfigureGrantManager();  // Extension method for entity configuration
    }
}

// Tenant database context (tenant-specific data)
[ConnectionStringName("GrantManager")]
[IgnoreMultiTenancy]  // This DbContext manages its own tenancy
public class GrantTenantDbContext : AbpDbContext<GrantTenantDbContext>
{
    public DbSet<GrantApplication> Applications { get; set; } = null!;
    public DbSet<GrantProgram> Programs { get; set; } = null!;
    public DbSet<Assessment> Assessments { get; set; } = null!;
    
    public GrantTenantDbContext(DbContextOptions<GrantTenantDbContext> options) 
        : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ConfigureGrantTenant();
    }
}
```

### Entity Configuration

**Use fluent API in extension methods:**

```csharp
public static class GrantManagerDbContextModelCreatingExtensions
{
    public static void ConfigureGrantTenant(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));
        
        builder.Entity<GrantApplication>(b =>
        {
            b.ToTable("GrantApplications");
            
            // Configure properties
            b.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(ApplicationConsts.MaxTitleLength);
            
            b.Property(x => x.Description)
                .HasMaxLength(ApplicationConsts.MaxDescriptionLength);
            
            // Configure indexes
            b.HasIndex(x => x.ProgramId);
            b.HasIndex(x => x.ApplicantId);
            b.HasIndex(x => x.Status);
            
            // Configure relationships
            b.HasOne<GrantProgram>()
                .WithMany()
                .HasForeignKey(x => x.ProgramId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Configure ABP features
            b.ConfigureByConvention();  // Configures audit properties, multi-tenancy, etc.
        });
    }
}
```

**Best Practices:**
- Separate entity configuration from DbContext class
- Use `ConfigureByConvention()` to apply ABP conventions
- Configure indexes on foreign keys and frequently queried fields
- Specify max lengths for string properties
- Use `DeleteBehavior.Restrict` for important relationships to prevent accidental cascading deletes
- Use table name pluralization (e.g., `GrantApplications`)

### Custom Repository Implementation

**Implement custom repositories in EntityFrameworkCore project:**

```csharp
public class ApplicationRepository : EfCoreRepository<GrantTenantDbContext, GrantApplication, Guid>, IApplicationRepository
{
    public ApplicationRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider) 
        : base(dbContextProvider) { }
    
    public virtual async Task<List<GrantApplication>> GetApplicationsByProgramAsync(
        Guid programId, 
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .Where(x => x.ProgramId == programId)
            .OrderByDescending(x => x.CreationTime)
            .ToListAsync(cancellationToken);
    }
    
    public virtual async Task<int> GetSubmittedCountByApplicantAsync(
        Guid applicantId, 
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .CountAsync(x => x.ApplicantId == applicantId && x.Status != ApplicationStatus.Draft, 
                        cancellationToken);
    }
}
```

## Testing Patterns

### Unit Testing (Application Layer)

**Use xUnit and Shouldly:**

```csharp
public class ApplicationAppService_Tests : GrantManagerApplicationTestBase
{
    private readonly IApplicationAppService _applicationAppService;
    private readonly IRepository<GrantApplication, Guid> _applicationRepository;
    
    public ApplicationAppService_Tests()
    {
        _applicationAppService = GetRequiredService<IApplicationAppService>();
        _applicationRepository = GetRequiredService<IRepository<GrantApplication, Guid>>();
    }
    
    [Fact]
    public async Task Should_Create_Application()
    {
        // Arrange
        var input = new CreateApplicationDto
        {
            Title = "Test Application",
            ProgramId = GrantManagerTestData.ProgramId
        };
        
        // Act
        var result = await _applicationAppService.CreateAsync(input);
        
        // Assert
        result.ShouldNotBeNull();
        result.Title.ShouldBe("Test Application");
        
        // Verify in database
        var application = await _applicationRepository.FindAsync(result.Id);
        application.ShouldNotBeNull();
        application!.Title.ShouldBe("Test Application");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Should_Not_Create_Application_With_Invalid_Title(string? invalidTitle)
    {
        // Arrange
        var input = new CreateApplicationDto
        {
            Title = invalidTitle!,
            ProgramId = GrantManagerTestData.ProgramId
        };
        
        // Act & Assert
        await Should.ThrowAsync<AbpValidationException>(async () =>
        {
            await _applicationAppService.CreateAsync(input);
        });
    }
}
```

**Best Practices:**
- Inherit from test base class that sets up DI container and database
- Use `[Fact]` for single test cases, `[Theory]` with `[InlineData]` for parameterized tests
- Use Shouldly assertions: `ShouldBe()`, `ShouldNotBeNull()`, `ShouldThrow()`, etc.
- Follow Arrange-Act-Assert pattern
- Use meaningful test method names: `Should_[Expected]_[Scenario]`
- Test both success and failure paths
- Clean up test data if not using transaction rollback

### Integration Testing (Domain Layer)

```csharp
public class ApplicationManager_Tests : GrantManagerDomainTestBase
{
    private readonly ApplicationManager _applicationManager;
    private readonly IRepository<GrantApplication, Guid> _applicationRepository;
    
    public ApplicationManager_Tests()
    {
        _applicationManager = GetRequiredService<ApplicationManager>();
        _applicationRepository = GetRequiredService<IRepository<GrantApplication, Guid>>();
    }
    
    [Fact]
    public async Task Should_Create_Application_When_Program_Is_Open()
    {
        // Arrange
        var programId = GrantManagerTestData.OpenProgramId;
        var applicantId = Guid.NewGuid();
        
        // Act
        var application = await _applicationManager.CreateAsync("Test", programId, applicantId);
        
        // Assert
        application.ShouldNotBeNull();
        application.Status.ShouldBe(ApplicationStatus.Draft);
    }
    
    [Fact]
    public async Task Should_Throw_When_Program_Is_Closed()
    {
        // Arrange
        var programId = GrantManagerTestData.ClosedProgramId;
        var applicantId = Guid.NewGuid();
        
        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessException>(async () =>
        {
            await _applicationManager.CreateAsync("Test", programId, applicantId);
        });
        
        exception.Message.ShouldContain("not currently accepting applications");
    }
}
```

## Database Migrations

### Creating Migrations

**Host Database (GrantManagerDbContext):**
```powershell
# Navigate to EntityFrameworkCore project
cd src/Unity.GrantManager.EntityFrameworkCore

# Add migration
dotnet ef migrations add AddUserPreferences --context GrantManagerDbContext

# Remove last migration if needed
dotnet ef migrations remove --context GrantManagerDbContext
```

**Tenant Database (GrantTenantDbContext):**
```powershell
# Navigate to EntityFrameworkCore project
cd src/Unity.GrantManager.EntityFrameworkCore

# Add migration
dotnet ef migrations add AddApplicationAttachments --context GrantTenantDbContext

# Remove last migration if needed
dotnet ef migrations remove --context GrantTenantDbContext
```

### Applying Migrations

**Use DbMigrator project:**
```powershell
# Set DbMigrator as startup project in Visual Studio
# Press Ctrl+F5 to run without debugging
# DbMigrator will apply all pending migrations to both host and tenant databases
```

**Best Practices:**
- Use descriptive migration names (e.g., `AddApplicationStatus`, `UpdateAssessmentSchema`)
- Review generated migration code before applying
- Never modify migration files after they've been applied in production
- Always test migrations on a copy of production data
- Keep migrations small and focused on single changes
- Add seed data in `GrantManagerDataSeedContributor` class, not in migrations

## Multi-Tenancy Guidelines

### Tenant-Aware Entities

```csharp
public class GrantApplication : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }  // Automatically set by ABP
    
    // ... other properties
}
```

**Best Practices:**
- Implement `IMultiTenant` for all tenant-specific entities
- Store tenant data in `GrantTenantDbContext` (separate database)
- ABP automatically filters queries by current tenant
- Use `[IgnoreMultiTenancy]` attribute on DbContext to manage tenant data manually
- Never manually filter by `TenantId` in queries (ABP does this automatically)

### Testing Multi-Tenancy

```csharp
[Fact]
public async Task Should_Only_Get_Current_Tenant_Applications()
{
    // Arrange - switch to specific tenant
    using (CurrentTenant.Change(GrantManagerTestData.TenantId))
    {
        // Act
        var applications = await _applicationRepository.GetListAsync();
        
        // Assert - all applications belong to current tenant
        applications.ShouldAllBe(x => x.TenantId == GrantManagerTestData.TenantId);
    }
}
```

## Event-Driven Architecture

### Publishing Domain Events

**Local Events (same database transaction):**
```csharp
public class GrantApplication : FullAuditedAggregateRoot<Guid>
{
    public void Submit()
    {
        Status = ApplicationStatus.Submitted;
        
        // Published when UnitOfWork commits
        AddLocalEvent(new ApplicationSubmittedEvent
        {
            ApplicationId = Id,
            Title = Title
        });
    }
}
```

**Distributed Events (RabbitMQ, cross-module):**
```csharp
public class GrantApplication : FullAuditedAggregateRoot<Guid>
{
    public void Approve()
    {
        Status = ApplicationStatus.Approved;
        
        // Published to RabbitMQ after UnitOfWork commits
        AddDistributedEvent(new ApplicationApprovedEto  // ETO = Event Transfer Object
        {
            ApplicationId = Id,
            Title = Title,
            ApplicantId = ApplicantId
        });
    }
}
```

### Handling Events

**Local Event Handler:**
```csharp
public class ApplicationSubmittedHandler : ILocalEventHandler<ApplicationSubmittedEvent>, ITransientDependency
{
    private readonly ILogger<ApplicationSubmittedHandler> _logger;
    
    public ApplicationSubmittedHandler(ILogger<ApplicationSubmittedHandler> logger)
    {
        _logger = logger;
    }
    
    public virtual async Task HandleEventAsync(ApplicationSubmittedEvent eventData)
    {
        _logger.LogInformation($"Application {eventData.ApplicationId} was submitted");
        
        // Handle within same transaction
        await Task.CompletedTask;
    }
}
```

**Distributed Event Handler:**
```csharp
public class ApplicationApprovedHandler : IDistributedEventHandler<ApplicationApprovedEto>, ITransientDependency
{
    private readonly IPaymentService _paymentService;
    
    public ApplicationApprovedHandler(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }
    
    public virtual async Task HandleEventAsync(ApplicationApprovedEto eventData)
    {
        // Create payment request in different module/database
        await _paymentService.CreatePaymentRequestAsync(eventData.ApplicationId);
    }
}
```

## Front-End Development

### Client-Side Package Management

Unity Grant Manager uses **NPM** for client-side package management with ABP's resource mapping system.

**Adding a new NPM package:**

1. Add dependency to `package.json`:
```json
{
  "dependencies": {
    "@abp/bootstrap": "^8.3.4",
    "datatables.net-bs5": "^1.13.6",
    "your-package-name": "^1.0.0"
  }
}
```

2. Run npm install:
```bash
npm install
```

3. Configure resource mapping in `abp.resourcemapping.js`:
```javascript
module.exports = {
    aliases: {
        '@node_modules': './node_modules',
        '@libs': './wwwroot/libs',
    },
    mappings: {
        '@node_modules/your-package/dist/': '@libs/your-package/',
    },
};
```

4. Run ABP CLI to copy resources:
```bash
abp install-libs
```

5. Add to bundle contributor in `Unity.Theme.UX2`:
```csharp
// UnityThemeUX2GlobalScriptContributor.cs
public override void ConfigureBundle(BundleConfigurationContext context)
{
    context.Files.AddIfNotContains("/libs/your-package/your-script.js");
}
```

**Best Practices:**
- Prefer `@abp/*` packages (e.g., `@abp/jquery`, `@abp/bootstrap`) for version consistency across modules
- Always use `AddIfNotContains()` to prevent duplicate script/style references
- Use `abp install-libs` after any `package.json` changes
- Map only necessary files (js, css, fonts) to reduce bundle size

### JavaScript Conventions

**File Organization:**
- Place page-specific JavaScript in `/Pages/[Feature]/[PageName].js`
- Place reusable utilities in `/wwwroot/scripts/` or theme modules
- Use IIFE pattern to avoid global scope pollution

**Standard Pattern:**
```javascript
(function ($) {
    var l = abp.localization.getResource('GrantManager');
    
    var dataTable = $('#MyTable').DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true,
        serverSide: true,
        paging: true,
        ajax: abp.libs.datatables.createAjax(
            acme.grantManager.myFeature.myService.getList
        ),
        columnDefs: [
            {
                title: l('Actions'),
                rowAction: {
                    items: [
                        {
                            text: l('Edit'),
                            action: function (data) {
                                editModal.open({ id: data.record.id });
                            }
                        },
                        {
                            text: l('Delete'),
                            confirmMessage: function (data) {
                                return l('DeleteConfirmationMessage', data.record.name);
                            },
                            action: function (data) {
                                acme.grantManager.myFeature.myService
                                    .delete(data.record.id)
                                    .then(function () {
                                        abp.notify.success(l('SuccessfullyDeleted'));
                                        dataTable.ajax.reload();
                                    });
                            }
                        }
                    ]
                }
            },
            {
                title: l('Name'),
                data: 'name'
            },
            {
                title: l('CreationTime'),
                data: 'creationTime',
                dataFormat: 'datetime'
            }
        ]
    }));
    
    var createModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'GrantManager/MyFeature/CreateModal',
        modalClass: 'myFeatureCreate'
    });
    
    createModal.onResult(function () {
        dataTable.ajax.reload();
    });
    
    $('#NewRecordButton').click(function (e) {
        e.preventDefault();
        createModal.open();
    });
    
})(jQuery);
```

### ABP Dynamic JavaScript API Client Proxies

ABP automatically generates JavaScript client proxies for your application services.

**How it works:**
- Application services inheriting from `ApplicationService` are auto-exposed as HTTP APIs
- `/Abp/ServiceProxyScript` endpoint generates JavaScript proxy functions
- Proxies follow namespace convention: `[moduleName].[namespace].[serviceName].[methodName]`

**Example Application Service:**
```csharp
public class ApplicationAppService : ApplicationService, IApplicationAppService
{
    public virtual async Task<PagedResultDto<ApplicationDto>> GetListAsync(GetApplicationListDto input)
    {
        // Implementation
    }
    
    public virtual async Task<ApplicationDto> CreateAsync(CreateApplicationDto input)
    {
        // Implementation
    }
}
```

**Generated JavaScript Proxy Usage:**
```javascript
// GET list
acme.grantManager.applications.application.getList({
    maxResultCount: 10,
    skipCount: 0,
    filter: 'search term'
}).then(function(result) {
    console.log(result.items);
    console.log(result.totalCount);
});

// POST create
acme.grantManager.applications.application.create({
    title: 'My Application',
    description: 'Description'
}).then(function(result) {
    abp.notify.success('Successfully created!');
});

// DELETE
acme.grantManager.applications.application
    .delete('3fa85f64-5717-4562-b3fc-2c963f66afa6')
    .then(function() {
        abp.notify.success('Successfully deleted!');
    });

// PUT update
acme.grantManager.applications.application
    .update('3fa85f64-5717-4562-b3fc-2c963f66afa6', {
        title: 'Updated Title'
    })
    .then(function(result) {
        abp.notify.success('Successfully updated!');
    });
```

**AJAX Options:**
You can override AJAX options by passing an additional parameter:
```javascript
acme.grantManager.applications.application
    .delete(id, {
        type: 'POST', // Override HTTP method
        dataType: 'json',
        success: function() {
            console.log('Custom success handler');
        }
    });
```

**Benefits:**
- Type-safe API calls (parameters match C# method signatures)
- Automatic error handling via `abp.ajax`
- No manual AJAX configuration needed
- Returns jQuery Deferred objects (`.then()`, `.catch()`, `.always()`)

### DataTables.net Integration

Unity Grant Manager uses DataTables.net 1.x with Bootstrap 5 styling.

**Basic DataTable Setup:**
```javascript
var dataTable = $('#MyTable').DataTable(abp.libs.datatables.normalizeConfiguration({
    processing: true,
    serverSide: true,
    paging: true,
    searching: true,
    autoWidth: false,
    scrollCollapse: true,
    order: [[1, "asc"]],
    ajax: abp.libs.datatables.createAjax(
        acme.grantManager.myService.getList,
        function () {
            return {
                filter: $('#SearchInput').val(),
                status: $('#StatusFilter').val()
            };
        }
    ),
    columnDefs: [
        {
            title: l('Actions'),
            rowAction: {
                items: [
                    {
                        text: l('Edit'),
                        visible: abp.auth.isGranted('GrantManager.Edit'),
                        action: function (data) {
                            editModal.open({ id: data.record.id });
                        }
                    }
                ]
            }
        },
        {
            title: l('Name'),
            data: 'name',
            orderable: true
        },
        {
            title: l('Status'),
            data: 'status',
            render: function (data) {
                return l('Enum:ApplicationStatus.' + data);
            }
        }
    ]
}));
```

**Advanced DataTable Features:**

1. **Custom Filters:**
```javascript
$('#SearchInput').on('input', function () {
    dataTable.ajax.reload();
});

$('#StatusFilter').change(function () {
    dataTable.ajax.reload();
});
```

2. **Row Selection:**
```javascript
var dataTable = $('#MyTable').DataTable({
    // ... other config
    select: {
        style: 'multi'
    }
});

$('#BulkDeleteButton').click(function () {
    var selectedRows = dataTable.rows({ selected: true }).data().toArray();
    // Process selected rows
});
```

3. **Export Buttons:**
```javascript
var dataTable = $('#MyTable').DataTable({
    // ... other config
    buttons: [
        {
            extend: 'excel',
            text: l('ExportToExcel'),
            exportOptions: {
                columns: ':visible'
            }
        },
        {
            extend: 'csv',
            text: l('ExportToCsv')
        }
    ]
});
```

4. **Column Visibility:**
```javascript
var dataTable = $('#MyTable').DataTable({
    // ... other config
    buttons: [
        {
            extend: 'colvis',
            text: l('ColumnVisibility')
        }
    ]
});
```

**DataTable Best Practices:**
- Always use `abp.libs.datatables.normalizeConfiguration()` for ABP integration
- Use `abp.libs.datatables.createAjax()` for automatic server-side pagination
- Leverage `rowAction` for action buttons with permission checks
- Use `dataFormat` property for date/datetime/boolean formatting
- Call `dataTable.ajax.reload()` after CRUD operations

### ABP Modal Manager

Use `abp.ModalManager` for consistent modal dialogs.

**Basic Modal Usage:**
```javascript
var createModal = new abp.ModalManager({
    viewUrl: abp.appPath + 'GrantManager/Applications/CreateModal',
    scriptUrl: abp.appPath + 'Pages/GrantManager/Applications/CreateModal.js',
    modalClass: 'applicationCreate'
});

createModal.onOpen(function () {
    console.log('Modal opened');
});

createModal.onResult(function (result) {
    abp.notify.success(l('SavedSuccessfully'));
    dataTable.ajax.reload();
});

createModal.onClose(function () {
    console.log('Modal closed');
});

$('#NewApplicationButton').click(function (e) {
    e.preventDefault();
    createModal.open();
});
```

**Modal Script Pattern (CreateModal.js):**
```javascript
abp.modals.applicationCreate = function () {
    var l = abp.localization.getResource('GrantManager');
    var _$form = null;
    var _$modal = null;
    
    this.init = function (modalManager, args) {
        _$modal = modalManager.getModal();
        _$form = modalManager.getForm();
        
        // Initialize form validation
        _$form.data('validator').settings.ignore = '';
        
        // Custom form logic
        $('#ProgramSelect').change(function () {
            var programId = $(this).val();
            // Load dynamic fields based on program
        });
    };
};
```

**Submitting Modal Forms:**
```csharp
// In Razor Page (CreateModal.cshtml.cs)
public async Task<IActionResult> OnPostAsync()
{
    await _applicationAppService.CreateAsync(Application);
    return NoContent(); // Return NoContent to close modal and trigger onResult
}
```

### ABP JavaScript Utilities

**Localization:**
```javascript
var l = abp.localization.getResource('GrantManager');
var message = l('WelcomeMessage');
var formatted = l('GreetingMessage', userName); // With parameters
```

**Notifications:**
```javascript
abp.notify.success('Operation completed successfully');
abp.notify.info('Information message');
abp.notify.warn('Warning message');
abp.notify.error('An error occurred');
```

**Confirmation Dialogs:**
```javascript
abp.message.confirm(
    'Are you sure you want to delete this item?',
    'Confirm Delete'
).then(function (confirmed) {
    if (confirmed) {
        // Perform delete
    }
});
```

**Busy Indicator:**
```javascript
abp.ui.setBusy('#MyForm');

// ... perform operation

abp.ui.clearBusy('#MyForm');
```

**AJAX Calls (when proxy not available):**
```javascript
abp.ajax({
    url: '/api/app/my-service/custom-endpoint',
    type: 'POST',
    data: JSON.stringify({ key: 'value' }),
    contentType: 'application/json'
}).then(function (result) {
    console.log(result);
});
```

**Authorization:**
```javascript
if (abp.auth.isGranted('GrantManager.Applications.Edit')) {
    // Show edit button
}
```

**Settings:**
```javascript
var settingValue = abp.setting.get('SettingName');
var intValue = abp.setting.getInt('NumericSetting');
var boolValue = abp.setting.getBoolean('BooleanSetting');
```

### DOM Event Handlers

ABP provides automatic initialization for common UI components via DOM event handlers.

**Auto-Initialized Components:**
- **Tooltips:** `data-bs-toggle="tooltip"`
- **Popovers:** `data-bs-toggle="popover"`
- **Datepickers:** `input.datepicker` or `input[type=date]`
- **AJAX Forms:** `data-ajaxForm="true"`
- **Autocomplete Selects:** `class="auto-complete-select"`

**Example - Autocomplete Select:**
```html
<select class="auto-complete-select form-control" 
        name="ProgramId"
        data-autocomplete-api-url="/api/app/program/lookup"
        data-autocomplete-display-property="name"
        data-autocomplete-value-property="id"
        data-autocomplete-items-property="items"
        data-autocomplete-filter-param-name="filter">
</select>
```

**Example - Confirmation Dialog:**
```html
<form data-confirm="Are you sure you want to submit?">
    <!-- Form fields -->
    <button type="submit">Submit</button>
</form>
```

**Example - AJAX Form:**
```html
<form data-ajaxForm="true" action="/MyController/MyAction">
    <!-- ABP will automatically handle AJAX submission -->
</form>
```

### JavaScript Best Practices

**DO:**
- ✅ Use ABP dynamic proxies instead of manual `$.ajax` calls
- ✅ Wrap code in IIFE to avoid global scope pollution: `(function ($) { ... })(jQuery);`
- ✅ Use `abp.localization` for all user-facing text
- ✅ Use `abp.notify` and `abp.message` for user feedback
- ✅ Use `abp.auth.isGranted()` for permission checks in UI
- ✅ Use `abp.ModalManager` for modal dialogs
- ✅ Use `abp.libs.datatables` helpers for DataTables integration
- ✅ Call `dataTable.ajax.reload()` after CRUD operations
- ✅ Use `abp.ui.setBusy()` for long-running operations

**DON'T:**
- ❌ Don't use global variables (use module pattern or IIFE)
- ❌ Don't hardcode text strings (use localization)
- ❌ Don't use `alert()` or `confirm()` (use `abp.notify` and `abp.message`)
- ❌ Don't manually construct API URLs (use dynamic proxies)
- ❌ Don't forget to handle errors in promise chains
- ❌ Don't bypass ABP's modal manager for modal dialogs
- ❌ Don't forget to check permissions before showing UI elements

## Common Pitfalls & Solutions

### ❌ Don't: Expose entities directly from application services
```csharp
public async Task<GrantApplication> GetAsync(Guid id)  // ❌ Wrong
{
    return await _applicationRepository.GetAsync(id);
}
```

### ✅ Do: Return DTOs
```csharp
public async Task<ApplicationDto> GetAsync(Guid id)  // ✅ Correct
{
    var application = await _applicationRepository.GetAsync(id);
    return ObjectMapper.Map<GrantApplication, ApplicationDto>(application);
}
```

### ❌ Don't: Put business logic in application services
```csharp
public async Task<ApplicationDto> CreateAsync(CreateApplicationDto input)  // ❌ Wrong
{
    var application = new GrantApplication(GuidGenerator.Create(), input.Title);
    
    // Complex validation logic here (should be in domain service)
    var program = await _programRepository.GetAsync(input.ProgramId);
    if (!program.IsAcceptingApplications())
        throw new BusinessException("...");
    
    await _applicationRepository.InsertAsync(application);
    return ObjectMapper.Map<GrantApplication, ApplicationDto>(application);
}
```

### ✅ Do: Use domain services for business logic
```csharp
public async Task<ApplicationDto> CreateAsync(CreateApplicationDto input)  // ✅ Correct
{
    // Delegate to domain service
    var application = await _applicationManager.CreateAsync(
        input.Title, 
        input.ProgramId, 
        CurrentUser.GetId());
    
    await _applicationRepository.InsertAsync(application);
    return ObjectMapper.Map<GrantApplication, ApplicationDto>(application);
}
```

### ❌ Don't: Use non-virtual methods
```csharp
public async Task<ApplicationDto> GetAsync(Guid id)  // ❌ Can't be overridden
{
    // ...
}
```

### ✅ Do: Make methods virtual for extensibility
```csharp
public virtual async Task<ApplicationDto> GetAsync(Guid id)  // ✅ Can be overridden
{
    // ...
}
```

### ❌ Don't: Manually filter by TenantId
```csharp
var applications = await _applicationRepository  // ❌ Wrong
    .GetListAsync(x => x.TenantId == CurrentTenant.Id);
```

### ✅ Do: Let ABP handle tenant filtering automatically
```csharp
var applications = await _applicationRepository.GetListAsync();  // ✅ Correct
// ABP automatically filters by current tenant
```

## Code Review Checklist

Before submitting a pull request, ensure:

- [ ] Code follows ABP Framework conventions and patterns
- [ ] All public methods are `virtual`
- [ ] Nullable reference types are handled correctly
- [ ] DTOs are used for application service inputs/outputs (not entities)
- [ ] Domain logic is in domain layer (entities/domain services)
- [ ] Application services orchestrate use cases (thin layer)
- [ ] Repository interfaces are defined only when custom queries needed
- [ ] Entity configurations use fluent API in extension methods
- [ ] Multi-tenant entities implement `IMultiTenant`
- [ ] Tests are written for new functionality (xUnit + Shouldly)
- [ ] Database migrations are created for schema changes
- [ ] Authorization attributes (`[Authorize]`) are applied
- [ ] Logging is added for important operations
- [ ] Exception handling uses `BusinessException` for domain errors
- [ ] Async/await is used consistently
- [ ] No nullable reference type warnings

## Resources

- [ABP Framework Documentation](https://docs.abp.io/en/abp/latest)
- [ABP Best Practices](https://docs.abp.io/en/abp/latest/Best-Practices)
- [Implementing Domain Driven Design (e-book)](https://abp.io/books/implementing-domain-driven-design)
- [ABP Community](https://community.abp.io/)
- [ABP GitHub Repository](https://github.com/abpframework/abp)
- [ARCHITECTURE.md](./ARCHITECTURE.md) - System architecture overview
- [PRODUCT.md](./PRODUCT.md) - Product vision and features
