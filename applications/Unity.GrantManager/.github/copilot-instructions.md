# Unity Grant Manager - GitHub Copilot Guidelines

This project follows **ABP Framework 9.1.3** architecture and conventions. Always refer to the project documentation and ABP best practices when generating code or providing assistance.

## Essential Project Context

* **[Product Vision and Goals](../PRODUCT.md)**: Understand the grant management platform's high-level vision, key features (grant programs, applicant portal, assessments, payments), and business objectives.
* **[System Architecture and Design Principles](../ARCHITECTURE.md)**: Comprehensive architecture overview including ABP Framework patterns, DDD layered structure, multi-tenancy design, module dependencies (with Mermaid diagrams), technology stack (PostgreSQL, EF Core, Redis, RabbitMQ, Keycloak), and deployment architecture.
* **[Contributing Guidelines](../CONTRIBUTING.md)**: Detailed coding conventions, ABP patterns, testing practices, multi-tenancy guidelines, and common pitfalls to avoid.

**Important**: Suggest updates to these documents if you find incomplete or conflicting information during your work.

## ABP Framework-Specific Patterns

### Application Architecture (DDD Layers)

**Follow ABP's layered architecture with strict dependencies:**

- **Domain.Shared**: Constants, enums, shared types (no dependencies)
- **Domain**: Entities, domain services (`*Manager`), repository interfaces (depends on Domain.Shared)
- **Application.Contracts**: Service interfaces, DTOs (depends on Domain.Shared only)
- **Application**: Service implementations (depends on Domain + Application.Contracts)
- **EntityFrameworkCore**: DbContext, repositories (depends on Domain only)
- **HttpApi**: API controllers (depends on Application.Contracts)
- **Web**: Razor Pages, UI components (depends on Application + HttpApi)

### Core Conventions

**Base Classes:**
- Application Services: Inherit from `ApplicationService`, implement interface from Application.Contracts
- Domain Services: Inherit from `DomainService`, use `Manager` suffix (e.g., `ApplicationManager`)
- Entities: Inherit from `FullAuditedAggregateRoot<TKey>` or `AuditedAggregateRoot<TKey>`
- API Controllers: Inherit from `AbpController`
- Repositories: Use `IRepository<TEntity, TKey>` or define custom interface when needed

**ABP Base Class Injected Properties (available in ApplicationService, DomainService, AbpController):**
- `GuidGenerator` — Use to create new entity IDs; never use `Guid.NewGuid()`
- `Clock` — Use `Clock.Now` instead of `DateTime.Now` or `DateTime.UtcNow`
- `CurrentUser` — Access authenticated user info (Id, Name, Email, Roles)
- `CurrentTenant` — Access current tenant context (Id, Name)
- `L` or `L["Key"]` — Localization shortcut
- `ObjectMapper` — AutoMapper-based DTO/entity mapping
- `Logger` — Structured logging via `ILogger<T>`
- `AuthorizationService` — Programmatic authorization checks
- `UnitOfWorkManager` — Manual unit-of-work control when needed

**Naming:**
- Domain Services: `*Manager` suffix (e.g., `AssessmentManager`, `PaymentManager`)
- Application Services: `*AppService` suffix (e.g., `ApplicationAppService`)
- DTOs: Use descriptive suffixes (`CreateApplicationDto`, `UpdateApplicationDto`, `ApplicationDto`)
- Event Transfer Objects: `*Eto` suffix for distributed events

**Methods:**
- All public methods MUST be `virtual` to allow overriding and extensibility
- Async methods MUST have `Async` suffix
- Use `protected virtual` instead of `private` for helper methods
- Application service methods: Use simple names (`GetAsync`, `GetListAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`) — do NOT embed entity name (e.g., use `GetAsync` not `GetApplicationAsync`)
- For `UpdateAsync`, pass `id` as separate parameter — do NOT include it inside the DTO

**Authorization:**
- Apply `[Authorize(PermissionName)]` attributes on application service methods
- Define permissions in `*Permissions` static class in Domain.Shared project

**DTOs vs Entities:**
- Application services MUST accept and return DTOs only, never entities
- Use `ObjectMapper` (AutoMapper) to map between entities and DTOs
- Define mapping profiles in `*AutoMapperProfile` class in Application project

### Dependency Injection Conventions

ABP auto-registers services using marker interfaces — do NOT use `services.AddScoped<>()` or `services.AddTransient<>()` manually for ABP services.

- `ITransientDependency` — Registered as transient (new instance per injection)
- `ISingletonDependency` — Registered as singleton
- `IScopedDependency` — Registered as scoped (per-request)
- ABP application services, domain services, and repositories are auto-registered — no manual registration needed

### Entity Constructor Conventions

- Always include a `protected` parameterless constructor for EF Core/ORM deserialization
- Public constructor should accept `Guid id` from `IGuidGenerator` (never call `Guid.NewGuid()` yourself)
- Use ABP's `Check.NotNullOrWhiteSpace()` and `Check.NotNull()` for constructor parameter validation
- Set required properties in the constructor; use internal/private setters to protect invariants

### Time Handling

- ALWAYS use `Clock.Now` (from ABP base classes) or inject `IClock` — never use `DateTime.Now` or `DateTime.UtcNow`
- This ensures consistent time handling and testability across the application

### BusinessException Patterns

- Use namespaced error codes: `"GrantManager:ApplicationAlreadyExists"`
- Map error codes to localization keys in `Localization/GrantManager/en.json` for user-friendly messages
- Use `.WithData("key", value)` to pass interpolation parameters to localized error messages

### Multi-Tenancy Patterns

**This application uses database-per-tenant isolation:**

- **GrantManagerDbContext**: Host database for global data (tenants, users, settings)
- **GrantTenantDbContext**: Tenant-specific data (applications, assessments, payments) - marked with `[IgnoreMultiTenancy]`
- Tenant entities MUST implement `IMultiTenant` interface
- NEVER manually filter by `TenantId` - ABP handles this automatically
- Store tenant data in `GrantTenantDbContext`, host data in `GrantManagerDbContext`
- Create separate migration streams for host and tenant databases

### Repository Usage

**Use generic repository by default:**
```csharp
private readonly IRepository<GrantApplication, Guid> _applicationRepository;
```

**Define custom repository interface ONLY when you need:**
- Complex queries not easily expressed with LINQ
- Specialized database operations
- Raw SQL queries or stored procedures

**Custom repositories:**
- Interface goes in Domain project
- Implementation goes in EntityFrameworkCore project
- Inherit from `EfCoreRepository<TDbContext, TEntity, TKey>`

**Repository method conventions:**
- Always pass `CancellationToken` as the last parameter
- Use `includeDetails: true` when you need navigation properties (default is `false`)
- Prefer `GetAsync(id)` over `FindAsync(id)` when entity must exist (throws `EntityNotFoundException`)
- Use `GetListAsync()` / `GetCountAsync()` for pagination; prefer `IQueryable` via `GetQueryableAsync()` for complex queries

### Domain Events

**Local Events (same transaction, same database):**
```csharp
AddLocalEvent(new ApplicationSubmittedEvent { ... });
```

**Distributed Events (RabbitMQ, cross-module communication):**
```csharp
AddDistributedEvent(new ApplicationApprovedEto { ... });
```

Use distributed events for communication between:
- Unity.GrantManager → Unity.Notifications (email notifications)
- Unity.GrantManager → Unity.Payments (payment processing)
- Unity.GrantManager → Unity.Reporting (analytics updates)

### Testing Conventions

**Framework:** xUnit + Shouldly

**Test Organization:**
- `*_Tests` suffix for test classes
- `Should_[Expected]_[Scenario]` for test method names
- Use `[Fact]` for single tests, `[Theory]` with `[InlineData]` for parameterized tests

**Assertions (use Shouldly):**
```csharp
result.ShouldNotBeNull();
result.Title.ShouldBe("Expected Value");
list.ShouldContain(x => x.Id == expectedId);
await Should.ThrowAsync<BusinessException>(() => ...);
```

**Base Classes:**
- Application tests: `GrantManagerApplicationTestBase`
- Domain tests: `GrantManagerDomainTestBase`
- Web tests: `GrantManagerWebTestBase`

### Database Migrations

**Two separate migration streams:**

**Host migrations:**
```bash
cd src/Unity.GrantManager.EntityFrameworkCore
dotnet ef migrations add MigrationName --context GrantManagerDbContext
```

**Tenant migrations:**
```bash
cd src/Unity.GrantManager.EntityFrameworkCore
dotnet ef migrations add MigrationName --context GrantTenantDbContext
```

**Apply migrations:** Run `Unity.GrantManager.DbMigrator` project (Ctrl+F5)

### Module Integration Patterns

**Direct service injection (synchronous, same process):**
```csharp
private readonly IFlexFieldService _flexFieldService;  // Unity.Flex module
```

**Distributed events (asynchronous, potentially different database):**
```csharp
// Publish
AddDistributedEvent(new ApplicationApprovedEto { ... });

// Handle in Unity.Payments module
public class ApplicationApprovedHandler : IDistributedEventHandler<ApplicationApprovedEto>
```

**Available Unity modules:**
- Unity.Flex: Dynamic forms and custom fields
- Unity.Notifications: CHES email service integration
- Unity.Payments: CAS payment system integration
- Unity.Reporting: Report generation and analytics
- Unity.Identity.Web: Custom identity UI
- Unity.TenantManagement: Multi-tenant administration
- Unity.Theme.UX2: BC Government UI theme
- Unity.SharedKernel: Cross-cutting utilities

## .NET 9.0 & C# 12 Conventions

**Language Features:**
- Nullable reference types are ENABLED project-wide
- Always declare nullability explicitly: `string?` vs `string`
- Use `null!` only when DI guarantees non-null (e.g., `public DbSet<Entity> Entities { get; set; } = null!;`)
- Target framework: `net9.0`
- Use latest C# features (primary constructors, collection expressions, etc.)

**Code Style:**
- Async methods: Always use `async/await`, suffix with `Async`
- Access modifiers: Always specify explicitly (`public`, `private`, `protected`)
- Indentation: 4 spaces, no tabs
- Braces: Always use, even for single-line statements

## Business Domain Understanding

**Core Entities:**
- Grant Programs: Configured by staff, define intake periods and requirements
- Applications: Submitted by applicants through portal
- Assessments: Review workflows with scoring by assessors
- Payments: Payment requests processed through CAS via Unity.Payments

**User Roles:**
- Applicants: Submit and track grant applications
- Program Officers: Configure programs, review applications
- Assessors: Score and evaluate applications
- Finance Staff: Process payments and manage budgets

**Integration Points:**
- CHES: Government email service for notifications
- CAS: Common Accounting System for payments
- Keycloak: Identity provider for authentication
- AWS S3: Document/blob storage

## ABP Framework Resources

**When you encounter ABP-specific questions, reference:**
- [ABP Best Practices](https://docs.abp.io/en/abp/latest/Best-Practices)
- [Module Architecture Guide](https://docs.abp.io/en/abp/latest/Best-Practices/Module-Architecture)
- [Application Services](https://docs.abp.io/en/abp/latest/Best-Practices/Application-Services)
- [Domain Services](https://docs.abp.io/en/abp/latest/Best-Practices/Domain-Services)
- [Entities](https://docs.abp.io/en/abp/latest/Best-Practices/Entities)
- [Repositories](https://docs.abp.io/en/abp/latest/Best-Practices/Repositories)
- [Multi-Tenancy](https://docs.abp.io/en/abp/latest/Multi-Tenancy)
- [ABP GitHub Repository](https://github.com/abpframework/abp)

## Common Mistakes to Avoid

### Backend Anti-Patterns
❌ **Don't expose entities from application services** — Always return DTOs
❌ **Don't put business logic in application services** — Use domain services (`*Manager`)
❌ **Don't use non-virtual methods** — All public methods must be virtual
❌ **Don't manually filter by TenantId** — ABP does this automatically
❌ **Don't create custom repositories unnecessarily** — Use `IRepository<TEntity, TKey>` first
❌ **Don't mix host and tenant data in same DbContext** — Separate contexts for isolation
❌ **Don't forget [Authorize] attributes** — Always check permissions
❌ **Don't ignore nullable warnings** — Fix them properly
❌ **Don't use `DateTime.Now` or `DateTime.UtcNow`** — Use `Clock.Now` or inject `IClock`
❌ **Don't use `Guid.NewGuid()`** — Use `GuidGenerator.Create()` from ABP base classes
❌ **Don't use `services.AddScoped<>()` for ABP services** — Use `ITransientDependency` / `IScopedDependency` marker interfaces
❌ **Don't call application services from other services in the same module** — Extract shared logic to a domain service
❌ **Don't inject `DbContext` directly** — Use repositories for all data access
❌ **Don't embed entity name in application service methods** — Use `GetAsync`, not `GetApplicationAsync`
❌ **Don't put `Id` inside update DTOs** — Pass `id` as a separate parameter to `UpdateAsync`

### Frontend Anti-Patterns
❌ **Don't use manual AJAX** — Use ABP's dynamic JavaScript proxies
❌ **Don't create global JavaScript variables** — Wrap in IIFE pattern
❌ **Don't hardcode strings in JavaScript** — Use `abp.localization`
❌ **Don't bypass ABP modal manager** — Use `abp.ModalManager` for modals
❌ **Don't forget DataTable reload** — Call `dataTable.ajax.reload()` after CRUD

## Front-End Development Patterns

### Client-Side Package Management

**Adding NPM packages:**
1. Add to `package.json` (prefer `@abp/*` packages for consistency)
2. Run `npm install`
3. Configure `abp.resourcemapping.js` to map resources from `node_modules` to `wwwroot/libs`
4. Run `abp install-libs` to copy resources
5. Add to bundle contributor in `Unity.Theme.UX2` module

**Example resource mapping:**
```javascript
// abp.resourcemapping.js
module.exports = {
    aliases: {
        '@node_modules': './node_modules',
        '@libs': './wwwroot/libs',
    },
    mappings: {
        '@node_modules/datatables.net-bs5/': '@libs/datatables.net-bs5/',
        '@node_modules/echarts/dist/echarts.min.js': '@libs/echarts/',
    },
};
```

### JavaScript Structure and Conventions

**Standard page script pattern:**
```javascript
(function ($) {
    var l = abp.localization.getResource('GrantManager');
    
    // DataTable initialization
    var dataTable = $('#MyTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            // Configuration
        })
    );
    
    // Modal initialization
    var createModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'GrantManager/MyFeature/CreateModal',
        modalClass: 'myFeatureCreate'
    });
    
    createModal.onResult(function () {
        dataTable.ajax.reload();
    });
    
    // Event handlers
    $('#NewRecordButton').click(function (e) {
        e.preventDefault();
        createModal.open();
    });
    
})(jQuery);
```

**Always:**
- Wrap in IIFE: `(function ($) { ... })(jQuery);`
- Use `var l = abp.localization.getResource('GrantManager');` for localization
- Use `abp.notify` for success/error messages
- Use `abp.message.confirm()` for confirmation dialogs
- Use `abp.auth.isGranted()` for permission checks

### ABP Dynamic JavaScript API Client Proxies

**How it works:**
- Application services are automatically exposed as JavaScript functions
- Namespace follows pattern: `[moduleName].[namespace].[serviceName].[methodName]()`
- Functions return jQuery Deferred objects (use `.then()`, `.catch()`)
- Auto-generated from `/Abp/ServiceProxyScript` endpoint

**Example usage:**
```javascript
// GET list
acme.grantManager.applications.application.getList({
    maxResultCount: 10,
    filter: 'search'
}).then(function(result) {
    console.log(result.items);
});

// POST create
acme.grantManager.applications.application.create({
    title: 'New Application'
}).then(function(result) {
    abp.notify.success(l('SavedSuccessfully'));
});

// DELETE
acme.grantManager.applications.application
    .delete(id)
    .then(function() {
        abp.notify.success(l('SuccessfullyDeleted'));
        dataTable.ajax.reload();
    });
```

**Benefits:**
- No manual AJAX configuration
- Type-safe (parameters match C# signatures)
- Automatic error handling
- Consistent with ABP conventions

### DataTables.net Integration

**Unity Grant Manager uses DataTables.net 2.x** with the Bootstrap 5 integration package (`datatables.net-bs5`). Ensure generated examples and APIs target DataTables 2.x.

**Standard DataTable pattern:**
```javascript
var dataTable = $('#MyTable').DataTable(abp.libs.datatables.normalizeConfiguration({
    processing: true,
    serverSide: true,
    paging: true,
    ajax: abp.libs.datatables.createAjax(
        acme.grantManager.myService.getList,
        function () {
            // Return additional filter parameters
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
                    },
                    {
                        text: l('Delete'),
                        confirmMessage: function (data) {
                            return l('DeleteConfirmationMessage', data.record.name);
                        },
                        action: function (data) {
                            acme.grantManager.myService
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
            dataFormat: 'datetime' // ABP auto-formatting
        }
    ]
}));
```

**Key patterns:**
- Use `abp.libs.datatables.normalizeConfiguration()` wrapper
- Use `abp.libs.datatables.createAjax()` for server-side pagination
- Use `rowAction` for action buttons with permission checks
- Use `dataFormat` property for automatic date/boolean formatting
- Call `dataTable.ajax.reload()` after CRUD operations

### ABP Modal Manager

**Creating modals:**
```javascript
var createModal = new abp.ModalManager({
    viewUrl: abp.appPath + 'GrantManager/Applications/CreateModal',
    scriptUrl: abp.appPath + 'Pages/GrantManager/Applications/CreateModal.js',
    modalClass: 'applicationCreate'
});

createModal.onResult(function () {
    abp.notify.success(l('SavedSuccessfully'));
    dataTable.ajax.reload();
});

$('#NewButton').click(function (e) {
    e.preventDefault();
    createModal.open();
});
```

**Modal script class (CreateModal.js):**
```javascript
abp.modals.applicationCreate = function () {
    var _$form = null;
    
    this.init = function (modalManager, args) {
        _$form = modalManager.getForm();
        
        // Custom initialization logic
        _$form.find('#ProgramId').change(function () {
            // Handle program change
        });
    };
};
```

**Closing modal after save:**
```csharp
// In Razor Page code-behind
public async Task<IActionResult> OnPostAsync()
{
    await _appService.CreateAsync(Model);
    return NoContent(); // Return NoContent to close modal and trigger onResult
}
```

### ABP JavaScript Utilities

**Localization:**
```javascript
var l = abp.localization.getResource('GrantManager');
var message = l('WelcomeMessage');
var formatted = l('GreetingMessage', userName);
```

**Notifications:**
```javascript
abp.notify.success('Success message');
abp.notify.error('Error message');
abp.notify.warn('Warning message');
abp.notify.info('Info message');
```

**Confirmation dialogs:**
```javascript
abp.message.confirm(
    'Are you sure?',
    'Confirm Action'
).then(function (confirmed) {
    if (confirmed) {
        // Perform action
    }
});
```

**Authorization:**
```javascript
if (abp.auth.isGranted('GrantManager.Applications.Edit')) {
    // Show edit button
}
```

**Busy indicator:**
```javascript
abp.ui.setBusy('#MyForm');
// ... operation ...
abp.ui.clearBusy('#MyForm');
```

### DOM Auto-Initialization

ABP automatically initializes these components via DOM event handlers:

- **Tooltips:** `data-bs-toggle="tooltip"`
- **Popovers:** `data-bs-toggle="popover"`
- **Datepickers:** `<input class="datepicker">` or `<input type="date">`
- **AJAX Forms:** `<form data-ajaxForm="true">`
- **Autocomplete Selects:** `<select class="auto-complete-select" data-autocomplete-api-url="...">`
- **Confirmation Forms:** `<form data-confirm="Confirmation message">`

**Example autocomplete:**
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

## Code Generation Guidelines

When generating code:

1. **Check layer dependencies** - Ensure proper dependency direction (Domain ← Application ← Web)
2. **Use ABP base classes** - Don't create from scratch what ABP provides
3. **Follow naming conventions** - Especially `*Manager` for domain services, `*AppService` for application services
4. **Make methods virtual** - Critical for extensibility and ABP conventions
5. **Use proper DTOs** - Never expose entities in API/application layer
6. **Apply multi-tenancy** - Implement `IMultiTenant` for tenant data
7. **Add authorization** - Include `[Authorize]` attributes with permission names
8. **Write tests** - Generate corresponding test class with xUnit/Shouldly
9. **Consider events** - Use distributed events for cross-module communication
10. **Document complex logic** - Add XML comments for public APIs

## When in Doubt

1. Check existing code patterns in the same layer/project
2. Refer to [ARCHITECTURE.md](../ARCHITECTURE.md) for architectural decisions
3. Consult [CONTRIBUTING.md](../CONTRIBUTING.md) for detailed patterns and examples
4. Review ABP Framework best practices documentation
5. Ask for clarification rather than making assumptions that violate ABP conventions
