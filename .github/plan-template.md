---
title: [Short descriptive title of the feature]
version: 1.0
date_created: [YYYY-MM-DD]
last_updated: [YYYY-MM-DD]
---

# Implementation Plan: [Feature Name]

## Overview

[Brief description of the feature requirements and business goals. Include the problem this feature solves and the value it provides to users.]

## Requirements Summary

### Functional Requirements
- [Requirement 1]
- [Requirement 2]
- [Requirement 3]

### Non-Functional Requirements
- [Performance, security, scalability considerations]
- [Compliance or regulatory requirements]
- [Integration requirements with external systems]

### User Stories (if applicable)
- As a [user role], I want to [action] so that [benefit]
- As a [user role], I want to [action] so that [benefit]

## Architecture and Design

### Affected ABP Layers
- [ ] **Domain Layer**: New entities, domain services, repository interfaces, domain events
- [ ] **Application Layer**: Application services, DTOs, AutoMapper profiles
- [ ] **EntityFrameworkCore Layer**: DbContext changes, repository implementations, entity configurations
- [ ] **HttpApi Layer**: API controllers, endpoints
- [ ] **Web Layer**: Razor Pages, view components, JavaScript, CSS

### Impacted Modules
- [ ] **Unity.GrantManager** (main application)
- [ ] **Unity.Flex** (dynamic forms)
- [ ] **Unity.Notifications** (email notifications)
- [ ] **Unity.Payments** (payment processing)
- [ ] **Unity.Reporting** (analytics/reports)
- [ ] **Unity.Identity.Web** (user management)
- [ ] **Unity.TenantManagement** (tenant admin)
- [ ] **Unity.SharedKernel** (utilities)

### Multi-Tenancy Considerations
- [ ] **Tenant-Scoped Data**: Will this feature store tenant-specific data?
  - If yes, entities must implement `IMultiTenant` and use `GrantTenantDbContext`
- [ ] **Host-Level Data**: Will this feature require global/host data?
  - If yes, use `GrantManagerDbContext` for cross-tenant entities
- [ ] **Tenant Isolation**: How will data isolation be enforced?
- [ ] **Cross-Tenant Operations**: Are there any scenarios where cross-tenant access is needed?

### Integration Points

#### Internal Modules
- [Describe how this feature integrates with Unity.Flex, Unity.Notifications, Unity.Payments, etc.]
- [Specify if domain events or distributed events are used for communication]

#### External Systems
- [ ] **CHES (Email Service)**: [Describe email notification requirements]
- [ ] **CAS (Payment System)**: [Describe payment integration needs]
- [ ] **Keycloak (Identity)**: [Describe authentication/authorization changes]
- [ ] **AWS S3 (Storage)**: [Describe document/file storage needs]

### Data Model Changes

#### New Entities
- **EntityName** (`GrantTenantDbContext` or `GrantManagerDbContext`)
  - Properties: [List key properties]
  - Relationships: [Describe foreign keys and navigation properties]
  - Aggregate Root: [Yes/No]

#### Modified Entities
- **EntityName**: [Describe changes - new properties, relationship changes, etc.]

#### Database Migrations
- [ ] Host migration required (`GrantManagerDbContext`)
- [ ] Tenant migration required (`GrantTenantDbContext`)

### API Design

#### New Endpoints
- `GET /api/grant-manager/[resource]` - [Description]
- `POST /api/grant-manager/[resource]` - [Description]
- `PUT /api/grant-manager/[resource]/{id}` - [Description]
- `DELETE /api/grant-manager/[resource]/{id}` - [Description]

#### Request/Response DTOs
- `Create[Entity]Dto`: [Key properties]
- `Update[Entity]Dto`: [Key properties]
- `[Entity]Dto`: [Key properties]

### UI/UX Changes

#### New Pages/Components
- [Page/Component Name]: [Description and purpose]

#### Modified Pages/Components
- [Page/Component Name]: [Description of changes]

#### User Flows
1. [Step-by-step user interaction flow]
2. [Include decision points and alternate paths]

### Security & Permissions

#### New Permissions
- `GrantManager.[Resource].Create`
- `GrantManager.[Resource].Edit`
- `GrantManager.[Resource].Delete`
- `GrantManager.[Resource].View`

#### Authorization Rules
- [Describe who can access what, role-based rules, data-level security]

### Events & Messaging

#### Domain Events (Local)
- `[Entity][Action]Event`: Triggered when [condition], handled by [handler]

#### Distributed Events (RabbitMQ)
- `[Entity][Action]Eto`: Published when [condition], consumed by [module/service]

### Performance Considerations
- [Indexing strategy for new database fields]
- [Caching strategy (Redis) if applicable]
- [Query optimization approaches]
- [Background job requirements (Quartz.NET)]

## Tasks

### Domain Layer Tasks
- [ ] Define `[Entity]` aggregate root in Domain project
  - [ ] Implement entity with proper encapsulation (private setters, business methods)
  - [ ] Add validation logic and business rules
  - [ ] Implement `IMultiTenant` if tenant-scoped
  - [ ] Add domain events if needed
- [ ] Create `[Entity]Manager` domain service (if complex business logic required)
  - [ ] Implement business logic methods
  - [ ] Add validation and business rule enforcement
  - [ ] Use `BusinessException` for domain errors
- [ ] Define `I[Entity]Repository` interface (if custom queries needed)
  - [ ] Specify custom query methods beyond standard CRUD
- [ ] Add constants to Domain.Shared project
  - [ ] String length constants
  - [ ] Enums for entity states/types

### Application Layer Tasks
- [ ] Define DTOs in Application.Contracts project
  - [ ] `Create[Entity]Dto` with validation attributes
  - [ ] `Update[Entity]Dto` with validation attributes
  - [ ] `[Entity]Dto` (output DTO)
  - [ ] List query DTOs (e.g., `Get[Entity]ListDto`)
- [ ] Define `I[Entity]AppService` interface in Application.Contracts
  - [ ] Standard CRUD methods: `GetAsync`, `GetListAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`
  - [ ] Custom methods for specific use cases
- [ ] Implement `[Entity]AppService` in Application project
  - [ ] Inject required repositories and domain services
  - [ ] Implement interface methods (all virtual)
  - [ ] Apply `[Authorize]` attributes for permissions
  - [ ] Use `ObjectMapper` for entity/DTO conversion
  - [ ] Handle pagination and filtering in `GetListAsync`
- [ ] Configure AutoMapper profile in Application project
  - [ ] Map entity to DTOs
  - [ ] Handle nested objects and value objects

### EntityFrameworkCore Layer Tasks
- [ ] Configure entity in `GrantManagerDbContextModelCreatingExtensions` or `GrantTenantDbContextModelCreatingExtensions`
  - [ ] Define table name
  - [ ] Configure properties (required, max length)
  - [ ] Configure indexes (foreign keys, frequently queried fields)
  - [ ] Configure relationships and foreign keys
  - [ ] Call `ConfigureByConvention()` for ABP features
- [ ] Implement custom repository (if `I[Entity]Repository` was defined)
  - [ ] Inherit from `EfCoreRepository<TDbContext, TEntity, TKey>`
  - [ ] Implement custom query methods
- [ ] Create database migration
  - [ ] Run `dotnet ef migrations add [MigrationName] --context [ContextName]`
  - [ ] Review generated migration code
  - [ ] Test migration on development database

### HttpApi Layer Tasks
- [ ] Create `[Entity]Controller` in HttpApi project
  - [ ] Inherit from `AbpController`
  - [ ] Inject `I[Entity]AppService`
  - [ ] Define route (`[Route("api/grant-manager/[resource]")]`)
  - [ ] Implement API endpoints (GET, POST, PUT, DELETE)
  - [ ] Return appropriate HTTP status codes

### Web Layer Tasks
- [ ] Create Razor Pages in Web project
  - [ ] Index page for listing entities
  - [ ] Create/Edit modal or page
  - [ ] Details page (if needed)
- [ ] Implement JavaScript functionality
  - [ ] AJAX calls to API endpoints
  - [ ] Client-side validation
  - [ ] DataTables integration for lists (if applicable)
- [ ] Add navigation menu items
  - [ ] Update main menu configuration
  - [ ] Apply permission checks for visibility
- [ ] Localization
  - [ ] Add localization keys to resource files
  - [ ] Translate to supported languages

### Testing Tasks
- [ ] Application service tests (xUnit + Shouldly)
  - [ ] Test CRUD operations
  - [ ] Test business logic validations
  - [ ] Test authorization (permission checks)
- [ ] Domain service tests
  - [ ] Test complex business rules
  - [ ] Test domain events
- [ ] Integration tests
  - [ ] Test API endpoints
  - [ ] Test multi-tenancy isolation (if applicable)

### Front-End Tasks
- [ ] Client-side package management (if new NPM packages needed)
  - [ ] Add dependencies to `package.json`
  - [ ] Configure `abp.resourcemapping.js` for resource mapping
  - [ ] Run `abp install-libs` to copy resources
  - [ ] Add to bundle contributor in `Unity.Theme.UX2`
- [ ] Page JavaScript implementation
  - [ ] Create page script in `/Pages/[Feature]/[PageName].js`
  - [ ] Wrap in IIFE pattern: `(function ($) { ... })(jQuery);`
  - [ ] Initialize localization: `var l = abp.localization.getResource('GrantManager');`
  - [ ] Configure DataTable with `abp.libs.datatables.normalizeConfiguration()`
  - [ ] Use ABP dynamic proxies for API calls (e.g., `acme.grantManager.myService.getList()`)
  - [ ] Implement modal managers for Create/Edit dialogs
  - [ ] Add event handlers for filters and buttons
  - [ ] Implement permission checks using `abp.auth.isGranted()`
- [ ] DataTable configuration
  - [ ] Define columns with localized titles
  - [ ] Configure row actions (Edit, Delete, custom actions)
  - [ ] Add permission checks to action visibility
  - [ ] Configure data formatting (datetime, boolean, enums)
  - [ ] Implement server-side pagination with `abp.libs.datatables.createAjax()`
  - [ ] Add custom filters (search, status, date ranges)
- [ ] Modal dialogs
  - [ ] Create modal Razor Pages (CreateModal.cshtml, EditModal.cshtml)
  - [ ] Implement modal script classes in `abp.modals.*` namespace
  - [ ] Configure modal manager with `viewUrl` and `modalClass`
  - [ ] Implement `onResult()` callback to reload DataTable
  - [ ] Return `NoContent()` from page handler to close modal
- [ ] Form validation and AJAX submission
  - [ ] Use `data-ajaxForm="true"` for AJAX forms
  - [ ] Implement client-side validation with jQuery Validation
  - [ ] Use `abp.notify` for success/error messages
  - [ ] Handle errors with `abp.message` or `abp.notify.error`
- [ ] Localization
  - [ ] Add localization keys to `Localization/GrantManager/en.json`
  - [ ] Use `l('LocalizationKey')` in JavaScript for all user-facing text
  - [ ] Test with multiple languages if multi-language support enabled
- [ ] UI/UX enhancements
  - [ ] Add tooltips with `data-bs-toggle="tooltip"`
  - [ ] Implement autocomplete selects with `class="auto-complete-select"`
  - [ ] Add busy indicators for long operations with `abp.ui.setBusy()`
  - [ ] Implement confirmation dialogs with `abp.message.confirm()`

### Documentation Tasks
- [ ] Update API documentation (Swagger annotations)
- [ ] Add XML comments to public APIs
- [ ] Update user documentation (if applicable)
- [ ] Document any configuration changes

## Implementation Sequence

**Recommended order to implement tasks:**

1. **Domain Layer** - Define entities, domain services, and core business logic
2. **Database Migration** - Create migration to support domain model
3. **Application Layer** - Implement use cases via application services and DTOs
4. **Testing** - Write and run tests for domain and application layers
5. **HttpApi Layer** - Expose API endpoints
6. **Web Layer** - Build UI pages and components
7. **Integration Testing** - Test end-to-end workflows
8. **Documentation** - Update all relevant documentation

## Open Questions

1. [Question about requirements, clarification needed on business rules, etc.]
2. [Question about technical approach, integration details, etc.]
3. [Question about edge cases, error handling, performance, etc.]

## Assumptions

- [Assumption 1 about system behavior, data availability, etc.]
- [Assumption 2 about user permissions, access patterns, etc.]

## Dependencies

### Blocking Dependencies
- [Prerequisite feature or task that must be completed first]

### Related Features
- [Features that should be coordinated or implemented together]

## Risks & Mitigation

| Risk | Impact | Probability | Mitigation Strategy |
|------|--------|-------------|---------------------|
| [Risk description] | High/Med/Low | High/Med/Low | [How to mitigate or handle] |

## Testing Strategy

### Unit Tests
- [Scope of unit testing - domain services, application services]

### Integration Tests
- [Scope of integration testing - database, external APIs]

### Manual Testing Scenarios
1. [Scenario 1: User action → Expected outcome]
2. [Scenario 2: User action → Expected outcome]

### Performance Testing
- [ ] Load testing (if applicable)
- [ ] Query performance validation
- [ ] Cache effectiveness verification

## Rollout Plan

### Feature Flags (if applicable)
- [ ] Enable/disable feature via ABP Feature Management
- [ ] Gradual rollout to tenants

### Data Migration
- [ ] Existing data migration requirements
- [ ] Backward compatibility considerations

### Deployment Steps
1. [Step 1]
2. [Step 2]
3. [Step 3]

## Success Criteria

- [ ] All functional requirements implemented and tested
- [ ] All tests passing (unit, integration, manual)
- [ ] Code review completed and approved
- [ ] Documentation updated
- [ ] Performance benchmarks met
- [ ] Security review completed (if applicable)
- [ ] Deployed to staging environment successfully
- [ ] User acceptance testing completed

## Notes

[Any additional notes, references, or context that doesn't fit in other sections]
