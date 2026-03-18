# ABP Framework Instructions for Unity Grant Manager

## Project Overview
Unity Grant Manager is an ASP.NET Core MVC application built using ABP Framework 9.1.3, following Domain-Driven Design (DDD) principles.

## Architecture & Technology Stack

### Backend
- **Framework**: ABP Framework 9.1.3 (ASP.NET Core MVC)
- **Architecture**: Domain-Driven Design (DDD)
- **Pattern**: Multi-layered application (Domain, Application, Web)
- **UI Framework**: ABP MVC UI with Bootstrap 4
- **ORM**: Entity Framework Core (inferred from ABP standard)

### Frontend
- **UI Theme**: ABP Basic Theme (`@abp/aspnetcore.mvc.ui.theme.basic`)
- **JavaScript**: jQuery, DataTables.net
- **Form Builder**: FormIO (formiojs 4.17.4)
- **Charts**: ECharts 6.0
- **Rich Text**: TinyMCE 8.3.2
- **CSS**: Bootstrap 4.6.2

## Project Structure

```
Unity.GrantManager/
├── src/
│   ├── Unity.GrantManager.Domain/           # Domain layer (entities, aggregates, repositories)
│   ├── Unity.GrantManager.Domain.Shared/    # Shared domain concepts
│   ├── Unity.GrantManager.Application/      # Application services
│   ├── Unity.GrantManager.Application.Contracts/ # DTOs, interfaces
│   ├── Unity.GrantManager.Web/              # MVC UI layer
│   │   ├── Views/                           # Razor views
│   │   │   └── Shared/Components/          # View components
│   │   ├── Controllers/                     # MVC controllers
│   │   ├── wwwroot/                        # Static files
│   │   └── Pages/                          # Razor pages
│   └── Unity.GrantManager.HttpApi/          # Web API controllers
├── test/                                    # Test projects
└── modules/                                 # ABP modules
```

## ABP Framework Conventions

### 1. Application Services
- Located in `*.Application` project
- Inherit from `ApplicationService` base class
- Use `AppService` suffix (e.g., `GrantApplicationAppService`)
- Return DTOs, not domain entities
- Handle authorization with `[Authorize]` attributes
- Use ABP's `IObjectMapper` for entity-to-DTO mapping

```csharp
public class GrantApplicationAppService : ApplicationService, IGrantApplicationAppService
{
    private readonly IRepository<GrantApplication, Guid> _repository;
    
    public GrantApplicationAppService(IRepository<GrantApplication, Guid> repository)
    {
        _repository = repository;
    }
    
    [Authorize(GrantManagerPermissions.GrantApplications.View)]
    public async Task<GrantApplicationDto> GetAsync(Guid id)
    {
        var entity = await _repository.GetAsync(id);
        return ObjectMapper.Map<GrantApplication, GrantApplicationDto>(entity);
    }
}
```

### 2. Domain Entities
- Located in `*.Domain` project
- Inherit from `Entity<TKey>`, `AggregateRoot<TKey>`, or `FullAuditedAggregateRoot<TKey>`
- Use `FullAuditedAggregateRoot` for entities requiring audit trails
- Place business logic in entity methods, not in services
- Use domain events for cross-aggregate communication

```csharp
public class GrantApplication : FullAuditedAggregateRoot<Guid>
{
    public string ReferenceNo { get; private set; }
    public decimal RequestedAmount { get; private set; }
    public ApplicationStatus Status { get; private set; }
    
    public void Approve(decimal approvedAmount)
    {
        // Business logic here
        Status = ApplicationStatus.Approved;
        AddDistributedEvent(new ApplicationApprovedEvent(Id));
    }
}
```

### 3. Repositories
- Use `IRepository<TEntity, TPrimaryKey>` for basic CRUD
- Create custom repositories in `*.Domain` for complex queries
- Repository interfaces in Domain, implementations in Infrastructure/EntityFrameworkCore

### 4. DTOs (Data Transfer Objects)
- Located in `*.Application.Contracts` project
- Separate DTOs for create, update, and read operations
- Use `EntityDto<TKey>` as base when including Id
- Example: `CreateGrantApplicationDto`, `UpdateGrantApplicationDto`, `GrantApplicationDto`

### 5. MVC Controllers
- Located in `*.Web` project's `Controllers` folder
- Inherit from `AbpController`
- Use dependency injection for application services
- Return `IActionResult` or derived types
- Use ABP's localization: `L["KeyName"]`

```csharp
public class GrantApplicationsController : AbpController
{
    private readonly IGrantApplicationAppService _applicationService;
    
    public GrantApplicationsController(IGrantApplicationAppService applicationService)
    {
        _applicationService = applicationService;
    }
    
    public async Task<IActionResult> Details(Guid applicationId)
    {
        var dto = await _applicationService.GetAsync(applicationId);
        return View(dto);
    }
}
```

### 6. View Components
- Located in `Views/Shared/Components/{ComponentName}/`
- Default view: `Default.cshtml`
- JavaScript file: `Default.js` (if needed)
- Invoke in views: `@await Component.InvokeAsync("ComponentName")`

### 7. Localization
- Use `L` function in C#: `L["KeyName"]`
- Use `l` function in JavaScript: `l('KeyName')`
- Use `@L["KeyName"]` in Razor views
- Localization files in JSON format in `Localization` folder

### 8. Permissions
- Define in `*Permissions.cs` files
- Use constants for permission names
- Check with `[Authorize(PermissionName)]` attribute or `await AuthorizationService.CheckAsync()`

## Unity Grant Manager Specific Patterns

### DataTables Integration
- Use `initializeDataTable()` helper function from `table-utils.js`
- Column definitions follow a consistent pattern with getter functions
- Enable server-side processing for large datasets
- Use `createNumberFormatter()` for currency formatting

```javascript
const dataTable = initializeDataTable({
    dt: $('#TableId'),
    defaultVisibleColumns: ['select', 'referenceNo', 'status'],
    listColumns: getColumns(formatter, l),
    maxRowsPerPage: 10,
    defaultSortColumn: { name: 'submissionDate', dir: 'desc' },
    dataEndpoint: service.getList,
    responseCallback: responseCallback,
    actionButtons: actionButtons,
    serverSideEnabled: true
});
```

### Column Getter Pattern
- Create separate functions for each column definition
- Include `columnIndex` parameter for ordering
- Return object with: `title`, `data`, `name`, `className`, `render`, `index`

```javascript
function getReferenceNoColumn(columnIndex) {
    return {
        title: 'Submission #',
        data: 'referenceNo',
        name: 'referenceNo',
        className: 'data-table-header text-nowrap',
        render: function (data, type, row) {
            return `<a href="/GrantApplications/Details?ApplicationId=${row.id}">${data || ''}</a>`;
        },
        index: columnIndex
    };
}
```

### Form Handling
- Use ABP's form validation helpers
- Leverage FormIO for dynamic forms
- Handle form submission via AJAX with proper error handling

### Date Handling
- Use `luxon` library for date manipulation
- Use ABP's `DateUtils.formatUtcDateToLocal()` helper
- Store dates in UTC, display in local timezone
- Format: `luxon.DateTime.fromISO(data).toUTC().toLocaleString()`

### Currency Formatting
```javascript
const formatter = createNumberFormatter(); // From table-utils.js
formatter.format(amount); // Returns formatted currency string
```

## Best Practices

### 1. Keep Business Logic in Domain
- Don't put business rules in controllers or views
- Use domain services for logic crossing multiple aggregates
- Application services orchestrate, domain entities execute

### 2. Use ABP Conventions
- Follow ABP naming conventions (`AppService`, `Dto`, etc.)
- Use ABP's built-in features (authorization, localization, audit logging)
- Leverage ABP's dependency injection

### 3. Maintain Layer Separation
- Domain layer has no dependencies on other layers
- Application layer depends only on Domain and Domain.Shared
- Web layer depends on Application.Contracts, not Domain directly

### 4. Error Handling
- Use ABP's `UserFriendlyException` for user-facing errors
- Use ABP's `BusinessException` for business rule violations
- Let ABP's exception handling middleware manage responses

### 5. JavaScript Organization
- Keep component-specific JS in component folders
- Extract reusable utilities to shared files (e.g., `table-utils.js`)
- Use function declarations for hoisted helpers
- Avoid duplicate function definitions

### 6. Testing
- Write unit tests for domain logic
- Integration tests for application services
- Use ABP's test infrastructure

## Common Operations

### Adding a New Entity
1. Create entity in `*.Domain` project
2. Add to `DbContext` in `*.EntityFrameworkCore`
3. Create migration
4. Create DTOs in `*.Application.Contracts`
5. Create application service in `*.Application`
6. Add AutoMapper mappings
7. Define permissions
8. Create MVC controller and views

### Database Migrations
```powershell
# From Unity.GrantManager.EntityFrameworkCore project directory
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Adding Localization Keys
1. Add to `en.json` in `Localization/GrantManager` folder
2. Add translations for other supported languages
3. Use `L["KeyName"]` in code

## Important Files & Utilities

### JavaScript Utilities
- `table-utils.js`: DataTables initialization and helpers
- `DateUtils`: Date formatting utilities
- `createNumberFormatter()`: Currency formatting

### Common JavaScript Patterns
```javascript
// Localization
const l = abp.localization.getResource('GrantManager');

// AJAX calls
abp.ajax({
    url: '/api/app/grant-application/...',
    type: 'POST',
    data: JSON.stringify(data)
});

// Notifications
abp.notify.success(l('SavedSuccessfully'));
abp.notify.error(l('ErrorOccurred'));
```

## ABP 9.1.3 Features for Unity Grant Manager

### 1. Background Jobs for Long-Running Operations
**Use Cases**: Application processing, bulk operations, report generation, email notifications

```csharp
// Define a background job
public class ProcessApplicationJob : AsyncBackgroundJob<ProcessApplicationArgs>
{
    private readonly IGrantApplicationRepository _repository;
    
    public ProcessApplicationJob(IGrantApplicationRepository repository)
    {
        _repository = repository;
    }
    
    public override async Task ExecuteAsync(ProcessApplicationArgs args)
    {
        var application = await _repository.GetAsync(args.ApplicationId);
        // Process application logic
    }
}

// Enqueue a job
await _backgroundJobManager.EnqueueAsync(new ProcessApplicationArgs { ApplicationId = id });
```

**Configuration** (in module class):
```csharp
Configure<AbpBackgroundJobOptions>(options =>
{
    options.IsJobExecutionEnabled = true; // Enable background job execution
});
```

### 2. Blob Storage for Document Management
**Use Cases**: Storing application documents, attachments, generated reports

```csharp
// Inject IBlobContainer
private readonly IBlobContainer<GrantApplicationDocuments> _blobContainer;

// Save a file
await _blobContainer.SaveAsync("document-name.pdf", stream, overrideExisting: true);

// Retrieve a file
var stream = await _blobContainer.GetAsync("document-name.pdf");

// Delete a file
await _blobContainer.DeleteAsync("document-name.pdf");
```

**Configuration** (module class):
```csharp
// Configure Blob Storage for different containers
Configure<AbpBlobStoringOptions>(options =>
{
    options.Containers.Configure<GrantApplicationDocuments>(container =>
    {
        container.UseFileSystem(fileSystem =>
        {
            fileSystem.BasePath = Path.Combine(hostingEnvironment.ContentRootPath, "Documents");
        });
    });
});
```

**Database Provider Alternative**:
```csharp
container.UseDatabase(); // Stores blobs in database
```

### 3. Global Feature System for Feature Toggles
**Use Cases**: Enable/disable features like assessment scoring, due diligence checks, payment processing

```csharp
// Define features in Domain.Shared
public static class GrantManagerFeatures
{
    public const string AdvancedScoring = "GrantManager.AdvancedScoring";
    public const string AutomatedDueDiligence = "GrantManager.AutomatedDueDiligence";
    public const string PaymentIntegration = "GrantManager.PaymentIntegration";
}

// Configure in module
GlobalFeatureManager.Instance.Modules.GrantManager()
    .EnableAll(); // Or .Enable(GrantManagerFeatures.AdvancedScoring)

// Check feature in code
if (await FeatureChecker.IsEnabledAsync(GrantManagerFeatures.AdvancedScoring))
{
    // Execute advanced scoring logic
}

// In Razor views
@if (await FeatureChecker.IsEnabledAsync(GrantManagerFeatures.PaymentIntegration))
{
    <button>Process Payment</button>
}
```

### 4. Distributed Events for Workflow Management
**Use Cases**: Application state changes, notifications, audit trail, integration with external systems

ABP 9.1.3 improves distributed event handling with better inbox/outbox pattern support.

```csharp
// Define event (in Domain.Shared)
[Serializable]
public class ApplicationApprovedEto : EtoBase
{
    public Guid ApplicationId { get; set; }
    public decimal ApprovedAmount { get; set; }
}

// Publish event (in Application Service or Domain Entity)
await _distributedEventBus.PublishAsync(new ApplicationApprovedEto 
{ 
    ApplicationId = id,
    ApprovedAmount = amount 
});

// Handle event (in Application layer)
public class ApplicationApprovedEventHandler : 
    IDistributedEventHandler<ApplicationApprovedEto>,
    ITransientDependency
{
    private readonly IEmailSender _emailSender;
    
    public ApplicationApprovedEventHandler(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }
    
    public async Task HandleEventAsync(ApplicationApprovedEto eventData)
    {
        // Send approval email
        // Create payment record
        // Update external systems
    }
}
```

**Configure Outbox for Reliability**:
```csharp
Configure<AbpDistributedEventBusOptions>(options =>
{
    options.Outboxes.Configure(config =>
    {
        config.UseDbContext<GrantManagerDbContext>();
    });
});
```

### 5. Enhanced Audit Logging
**Use Cases**: Track all changes to grant applications, compliance reporting, user activity monitoring

ABP 9.1.3 provides better audit log filtering and querying.

```csharp
// Disable auditing for specific method
[DisableAuditing]
public async Task<byte[]> GetLargeReportAsync()
{
    // Method not audited
}

// Custom audit log properties
public class GrantApplicationAppService : ApplicationService
{
    public async Task ApproveAsync(Guid id, decimal amount)
    {
        // Add custom audit data
        AuditingManager.Current.Log.EntityChanges.Add(new EntityChangeInfo
        {
            ChangeType = EntityChangeType.Updated,
            EntityId = id.ToString(),
            PropertyChanges = new List<EntityPropertyChangeInfo>
            {
                new EntityPropertyChangeInfo
                {
                    PropertyName = "ApprovalAmount",
                    NewValue = amount.ToString(),
                    OriginalValue = "0"
                }
            }
        });
    }
}

// Query audit logs (in a service)
var auditLogs = await _auditLogRepository.GetListAsync(
    includeDetails: true,
    httpMethod: "POST",
    url: "/api/app/grant-application",
    userName: "admin",
    startTime: DateTime.UtcNow.AddDays(-7),
    endTime: DateTime.UtcNow
);
```

### 6. Setting Management for Configurable Parameters
**Use Cases**: Approval thresholds, deadline configurations, scoring weights, notification preferences

```csharp
// Define settings (in Domain.Shared)
public static class GrantManagerSettings
{
    public const string ApprovalThreshold = "GrantManager.ApprovalThreshold";
    public const string MaxApplicationsPerUser = "GrantManager.MaxApplicationsPerUser";
    public const string AutoCloseDeadlineDays = "GrantManager.AutoCloseDeadlineDays";
}

// Define setting definition provider
public class GrantManagerSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition(
                GrantManagerSettings.ApprovalThreshold,
                "100000",
                isVisibleToClients: true,
                isEncrypted: false
            ),
            new SettingDefinition(
                GrantManagerSettings.MaxApplicationsPerUser,
                "5",
                isVisibleToClients: true
            )
        );
    }
}

// Use settings in code
var threshold = await SettingProvider.GetAsync<decimal>(GrantManagerSettings.ApprovalThreshold);

if (amount > threshold)
{
    // Require additional approval
}

// Get setting in JavaScript
var maxApps = await abp.setting.get('GrantManager.MaxApplicationsPerUser');
```

### 7. Dynamic Claims for Custom Authorization
**Use Cases**: Department-based access, region-based filtering, role-based data visibility

```csharp
// Define custom claim type
public static class GrantManagerClaims
{
    public const string Department = "GrantManager_Department";
    public const string Region = "GrantManager_Region";
    public const string MaxApprovalAmount = "GrantManager_MaxApprovalAmount";
}

// Add dynamic claims (in Identity module)
public class GrantManagerClaimsPrincipalContributor : IAbpClaimsPrincipalContributor, ITransientDependency
{
    public async Task ContributeAsync(AbpClaimsPrincipalContributorContext context)
    {
        var identity = context.ClaimsPrincipal.Identities.FirstOrDefault();
        var userId = identity?.FindUserId();
        
        if (userId.HasValue)
        {
            // Add custom claims from user profile or database
            var userDepartment = await GetUserDepartmentAsync(userId.Value);
            identity?.AddClaim(new Claim(GrantManagerClaims.Department, userDepartment));
        }
    }
}

// Use in authorization
[Authorize]
public async Task<List<GrantApplicationDto>> GetMyDepartmentApplicationsAsync()
{
    var department = CurrentUser.FindClaimValue(GrantManagerClaims.Department);
    return await _repository.GetListAsync(x => x.Department == department);
}
```

### 8. EF Core 8 Features (if using .NET 8+)
**New Capabilities**: JSON columns, raw SQL queries, complex type mapping

```csharp
// JSON column mapping (for flexible metadata)
public class GrantApplication : FullAuditedAggregateRoot<Guid>
{
    public string ReferenceNo { get; set; }
    public ApplicationMetadata Metadata { get; set; } // Stored as JSON
}

// In DbContext configuration
protected override void OnModelCreating(ModelBuilder builder)
{
    builder.Entity<GrantApplication>(b =>
    {
        b.OwnsOne(e => e.Metadata, b => b.ToJson());
    });
}

// Raw SQL queries with better performance
var applications = await _dbContext.Database
    .SqlQuery<GrantApplicationDto>($"EXEC GetTopApplications @Year = {year}")
    .ToListAsync();
```

### 9. Object Extension System for Extensibility
**Use Cases**: Add custom fields without modifying core entities

```csharp
// Configure in EntityFrameworkCore module
ObjectExtensionManager.Instance
    .AddOrUpdateProperty<GrantApplication, string>(
        "CustomField1",
        options => { options.MapEfCore(b => b.HasMaxLength(128)); }
    );

// Use in application service
application.SetProperty("CustomField1", "CustomValue");
var value = application.GetProperty<string>("CustomField1");
```

### 10. Text Template Management
**Use Cases**: Email templates, document generation, notification templates

```csharp
// Define template
public class ApprovalEmailTemplate : TemplateDefinitionProvider
{
    public override void Define(ITemplateDefinitionContext context)
    {
        context.Add(
            new TemplateDefinition("ApprovalEmail")
                .WithVirtualFilePath("/Templates/ApprovalEmail.tpl", isInlineLocalized: true)
        );
    }
}

// Use template
var emailBody = await _templateRenderer.RenderAsync(
    "ApprovalEmail",
    new { ApplicantName = "John Doe", Amount = 50000 }
);
```

## Module Structure
Unity Grant Manager includes:
- **Unity.Shared**: Shared components across Unity applications
- **MessageBrokers**: RabbitMQ integration (consider using ABP distributed events)
- **modules/**: Various ABP modules

## Additional Resources
- ABP Framework Documentation: https://docs.abp.io
- ABP 9.1 Release Notes: https://docs.abp.io/en/abp/9.1/Release-Info
- Project README: `/Unity/applications/Unity.GrantManager/README.md`
- Architecture documentation: `/Unity/documentation/`

## Recommended Next Steps for ABP 9.1.3 Integration

1. **Implement Blob Storage** for document management (replace file system storage)
2. **Add Distributed Events** for application workflow state changes
3. **Configure Background Jobs** for report generation and notifications
4. **Use Setting Management** for configurable business rules (thresholds, deadlines)
5. **Leverage Global Features** for feature flags in production
6. **Enhance Audit Logging** for compliance requirements
7. **Implement Dynamic Claims** for department/region-based access control
8. **Use Text Templates** for standardized email and document generation

---

**Remember**: This is an ABP Framework MVC application, NOT Angular. Use Razor views, jQuery, and traditional server-side rendering patterns.
