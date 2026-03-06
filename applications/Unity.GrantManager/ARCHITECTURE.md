# Unity Grant Manager - System Architecture

## Overview

Unity Grant Manager is built on **ABP Framework 9.1.3**, following Domain-Driven Design (DDD) principles and implementing a modular monolith architecture. The application leverages ABP's opinionated architecture to build enterprise-grade grant management software with clean separation of concerns, multi-tenancy support, and extensible module design.

## Technology Stack

### Core Framework & Runtime
- **.NET 9.0**: Latest .NET platform with C# 12.0 and nullable reference types enabled
- **ABP Framework 9.1.3**: Application framework providing DDD infrastructure, modularity, and multi-tenancy
- **ASP.NET Core MVC**: Web application framework with Razor Pages for server-side rendering

### Data & Persistence
- **PostgreSQL**: Primary relational database management system
- **Entity Framework Core 9.0.5**: ORM for data access with Npgsql provider
- **Redis**: Distributed caching and data protection key storage
- **Common Object Management Service (COMS)**: Blob storage for document management

### Front-End & UI
- **Unity.Theme.UX2**: Custom theme module for consistent government branding
- **Bootstrap 5**: UI component framework
- **jQuery**: JavaScript utilities and DOM manipulation
- **Bundling & Minification**: ABP bundling system for client-side resource optimization

### Messaging & Background Jobs
- **RabbitMQ**: Message broker for event bus and distributed event handling
- **Quartz.NET**: Background job scheduling and execution with clustering support

### Authentication & Authorization
- **Keycloak**: Identity provider for OpenID Connect authentication
- **ABP Identity**: User and role management infrastructure

### Testing & Quality
- **xUnit**: Test framework for unit and integration tests
- **Shouldly**: Fluent assertion library
- **MiniProfiler**: Performance profiling and diagnostics

### Logging & Monitoring
- **Serilog**: Structured logging with multiple sink support
- **ABP Audit Logging**: Comprehensive audit trail for all system operations

## Architectural Patterns

### Domain-Driven Design (DDD)

Unity Grant Manager follows DDD tactical patterns as prescribed by ABP Framework:

- **Entities & Aggregate Roots**: Core business objects with identity and lifecycle
- **Value Objects**: Immutable objects defined by their attributes
- **Domain Services**: Business logic that doesn't naturally fit within entities (suffix: `Manager`)
- **Repositories**: Abstract data access with `IRepository<TEntity, TKey>` pattern
- **Domain Events**: Decouple domain logic and enable event-driven architecture
- **Application Services**: Use case orchestration layer (inherit from `ApplicationService`)
- **Data Transfer Objects (DTOs)**: API contract objects for input/output

### Multi-Tenancy Architecture

Unity Grant Manager implements multi-tenancy with **database-per-tenant isolation**:

```mermaid
graph TB
    subgraph "Multi-Tenant Data Architecture"
        WebApp[Web Application]
        HostDb[(Host Database<br/>GrantManagerDbContext)]
        TenantDb1[(Tenant 1 Database<br/>GrantTenantDbContext)]
        TenantDb2[(Tenant 2 Database<br/>GrantTenantDbContext)]
        TenantDb3[(Tenant N Database<br/>GrantTenantDbContext)]
        
        WebApp -->|Host Data<br/>Tenants, Users, Settings| HostDb
        WebApp -->|Tenant 1 Data<br/>Applications, Assessments| TenantDb1
        WebApp -->|Tenant 2 Data<br/>Applications, Assessments| TenantDb2
        WebApp -->|Tenant N Data<br/>Applications, Assessments| TenantDb3
    end
    
    style HostDb fill:#e1f5ff
    style TenantDb1 fill:#fff4e1
    style TenantDb2 fill:#fff4e1
    style TenantDb3 fill:#fff4e1
```

**Key Components:**
- **GrantManagerDbContext**: Host database context for shared/global data (tenants, users, global settings)
- **GrantTenantDbContext**: Tenant-specific database context with `[IgnoreMultiTenancy]` attribute for tenant-scoped entities
- **Separate Migrations**: Distinct migration streams for host and tenant databases
- **Tenant Resolver**: Automatically determines current tenant from request context (URL, header, or claims)

## Module Architecture

Unity Grant Manager follows ABP's modular architecture with internal and external modules:

### Module Dependency Graph

```mermaid
---
config:
  layout: elk
  theme: redux
  htmlLabels: true
title: Module Dependency Graph
---
flowchart TB
 subgraph subGraph0["Unity Grant Manager Application"]
        Web["Unity.GrantManager.Web<br>Razor Pages and UI"]
        HttpApi["Unity.GrantManager.HttpApi<br>REST API Controllers"]
        App["Unity.GrantManager.Application<br>Application Services"]
        AppContracts["Unity.GrantManager.Application.Contracts<br>Service Interfaces, DTOs"]
        Domain["Unity.GrantManager.Domain<br>Entities, Repositories, Domain Services"]
        DomainShared["Unity.GrantManager.Domain.Shared<br>Enums, Constants"]
        EFCore["Unity.GrantManager.EntityFrameworkCore<br>DbContext, Repositories, EF Config"]
  end
 subgraph subGraph1["Unity Platform Modules"]
        Flex["Unity.Flex<br>Dynamic Forms"]
        Notifications["Unity.Notifications<br>CHES Email Integration"]
        Payments["Unity.Payments<br>CAS Payment Integration"]
        Reporting["Unity.Reporting<br>Report Generation"]
        Identity["Unity.Identity.Web<br>Identity UI"]
        Tenant["Unity.TenantManagement<br>Tenant Admin"]
        Theme["Unity.Theme.UX2<br>UI Theme"]
        SharedKernel["Unity.SharedKernel<br>Utilities, Message Brokers"]
  end
    Web --> HttpApi & App & Theme & Identity & EFCore
    HttpApi --> AppContracts
    App --> AppContracts & Domain & Flex & Notifications & Payments & Reporting & SharedKernel
    AppContracts --> DomainShared
    Domain --> DomainShared
    EFCore --> Domain

     Web:::GrantApp
     HttpApi:::GrantApp
     App:::GrantApp
     AppContracts:::GrantApp
     Domain:::GrantApp
     DomainShared:::GrantApp
     EFCore:::GrantApp
     Flex:::Platform
     Notifications:::Peach
     Payments:::Peach
     Reporting:::Platform
     Identity:::Platform
     Tenant:::Platform
     Theme:::Platform
     SharedKernel:::Platform
    classDef GrantApp fill:#BBDEFB,stroke:#000000,stroke-width:4px,color:#0D47A1
    classDef Platform fill:#C8E6C9,stroke:#2E7D32,stroke-width:4px,color:#1B5E20
    classDef Peach fill:#FFEFDB,stroke:#FBB35A,stroke-width:4px,color:#8F632D
    classDef Neutral fill:#E0E0E0,stroke:#757575,stroke-width:4px,color:#424242
    style App stroke:#000000
```

### Module Descriptions

#### Unity.GrantManager (Main Application)
The core grant management application implementing grant programs, applications, assessments, and related business logic.

**Layers:**
- **Web**: Razor Pages, view components, client-side assets, MVC controllers for UI
- **HttpApi**: RESTful API controllers extending `AbpController`
- **Application**: Application services implementing business use cases, inheriting from `ApplicationService`
- **Application.Contracts**: Service interfaces, DTOs, and application-layer contracts
- **Domain**: Entities (applications, assessments, programs), domain services, repository interfaces
- **Domain.Shared**: Enums, constants, shared types
- **EntityFrameworkCore**: EF Core DbContexts (`GrantManagerDbContext`, `GrantTenantDbContext`), repository implementations, entity configurations

#### Unity.Flex (Dynamic Forms Module)
Provides dynamic form/field definition and rendering capabilities for customizable grant application forms.

**Key Features:**
- Custom field definitions with validation rules
- Form layout and section management
- Runtime form rendering with data binding
- Field value storage and retrieval

**Integration:** Grant application forms are built using Flex definitions, allowing program administrators to customize intake forms without code changes.

#### Unity.Notifications (Notification Module)
Handles email notifications through CHES (Common Hosted Email Service) integration.

**Key Features:**
- Email template management
- CHES API integration for government email delivery
- Notification queue and retry logic
- Notification history and tracking

**Integration:** Triggered by domain events from Grant Manager (application submitted, assessment completed, payment processed) to send automated email notifications.

#### Unity.Payments (Payment Processing Module)
Integrates with CAS (Common Accounting System) for government payment processing.

**Key Features:**
- CAS API integration for payment submission
- Payment status tracking and reconciliation
- Invoice generation and management
- Payment approval workflows

**Integration:** Grant Manager creates payment requests for approved applications, which are processed through Unity.Payments to CAS.

#### Unity.Reporting (Reporting Module)
Advanced reporting and analytics capabilities.

**Key Features:**
- Custom report definitions
- Data visualization and dashboards
- Report scheduling and distribution
- Export formats (PDF, Excel, CSV)

**Integration:** Provides reporting on grant applications, assessment outcomes, payment distributions, and program performance.

#### Unity.Identity.Web (Identity UI Module)
Custom user interface for identity management operations.

**Key Features:**
- User registration and profile management
- Login/logout pages with Keycloak integration
- Password reset and account recovery
- Organization/team management UI

#### Unity.TenantManagement (Tenant Management Module)
Multi-tenant administration interface.

**Key Features:**
- Tenant creation and configuration
- Database connection string management
- Tenant-specific feature toggles
- Tenant user assignments

#### Unity.Theme.UX2 (UI Theme Module)
Consistent government branding and user experience.

**Key Features:**
- BC Government visual identity compliance
- Responsive layouts and components
- Accessibility (WCAG 2.1 AA) compliance
- Reusable UI components and patterns

#### Unity.SharedKernel (Shared Utilities Module)
Cross-cutting utilities and infrastructure shared across modules.

**Key Features:**
- HTTP client factories and helpers
- RabbitMQ message broker configuration
- Correlation ID propagation for distributed tracing
- Feature flags and utilities
- Integration abstractions

### Module Communication Patterns

```mermaid
sequenceDiagram
    participant User
    participant GrantManager
    participant Flex
    participant Notifications
    participant Payments
    participant RabbitMQ
    
    User->>GrantManager: Submit Grant Application
    GrantManager->>Flex: Validate Form Data
    Flex-->>GrantManager: Validation Result
    GrantManager->>GrantManager: Create Application Entity
    GrantManager->>RabbitMQ: Publish ApplicationSubmittedEvent
    
    RabbitMQ->>Notifications: ApplicationSubmittedEvent
    Notifications->>Notifications: Generate Email from Template
    Notifications->>CHES: Send Confirmation Email
    CHES-->>Notifications: Email Sent
    
    Note over GrantManager: Assessment Process...
    
    GrantManager->>RabbitMQ: Publish ApplicationApprovedEvent
    RabbitMQ->>Payments: ApplicationApprovedEvent
    Payments->>Payments: Create Payment Request
    Payments->>CAS: Submit Payment
    CAS-->>Payments: Payment Confirmation
    
    Payments->>RabbitMQ: Publish PaymentProcessedEvent
    RabbitMQ->>GrantManager: PaymentProcessedEvent
    GrantManager->>GrantManager: Update Application Status
    
    RabbitMQ->>Notifications: PaymentProcessedEvent
    Notifications->>CHES: Send Payment Notification
```

**Communication Mechanisms:**
1. **Direct Service References**: Modules can directly inject and call services from dependent modules (e.g., GrantManager → Flex for form validation)
2. **Domain Events (Local)**: In-process events for same-database transactions using ABP's `ILocalEventBus`
3. **Distributed Events (RabbitMQ)**: Cross-module/cross-database events using ABP's `IDistributedEventBus` with RabbitMQ transport
4. **HTTP APIs**: RESTful APIs for external integrations or microservice scenarios

## Layer Structure & Dependencies

Unity Grant Manager follows ABP's layered architecture with strict dependency rules:

```mermaid
graph TD
    subgraph "Presentation Layer"
        UI[Web]
    end
    
    subgraph "API Layer"
        HttpApi[HttpApi<br/>Controllers]
        HttpApiClient[HttpApi.Client<br/>C# API Proxies]
    end
    
    subgraph "Application Layer"
        App[Application<br/>Services Implementation]
        AppContracts[Application.Contracts<br/>Interfaces & DTOs]
    end
    
    subgraph "Domain Layer"
        Domain[Domain<br/>Entities, Domain Services, Repositories]
        DomainShared[Domain.Shared<br/>Constants, Enums]
    end
    
    subgraph "Infrastructure Layer"
        EFCore[EntityFrameworkCore<br/>DbContext, Repositories]
    end
    
    UI --> HttpApi
    UI --> App
    UI --> AppContracts
    HttpApi --> AppContracts
    HttpApiClient --> AppContracts
    App --> AppContracts
    App --> Domain
    AppContracts --> DomainShared
    Domain --> DomainShared
    EFCore --> Domain
    
    style UI fill:#4a90e2
    style App fill:#7b68ee
    style Domain fill:#50c878
    style EFCore fill:#f4a460
```

### Dependency Rules

1. **Domain Layer** has no dependencies on other layers (only on ABP framework)
2. **Application.Contracts** depends only on **Domain.Shared**
3. **Application** depends on **Domain** and **Application.Contracts**
4. **Infrastructure** (EF Core) depends on **Domain** only
5. **HttpApi** depends on **Application.Contracts**
6. **Web** can depend on any layer for hosting, but business logic stays in Application/Domain

### Project Dependencies (Actual)

**Unity.GrantManager.Web** depends on:
- Unity.GrantManager.Application
- Unity.GrantManager.HttpApi
- Unity.GrantManager.EntityFrameworkCore
- Unity.Theme.UX2
- Unity.Identity.Web

**Unity.GrantManager.Application** depends on:
- Unity.GrantManager.Application.Contracts
- Unity.GrantManager.Domain
- Unity.Flex
- Unity.Notifications
- Unity.Payments
- Unity.Reporting
- Unity.SharedKernel

**Unity.GrantManager.Domain** depends on:
- Unity.GrantManager.Domain.Shared
- Volo.Abp.Identity.Domain
- Volo.Abp.TenantManagement.Domain
- Volo.Abp.AuditLogging.Domain

**Unity.GrantManager.EntityFrameworkCore** depends on:
- Unity.GrantManager.Domain
- Volo.Abp.EntityFrameworkCore.PostgreSql

## Data Flow & Request Pipeline

### Typical Request Flow

```mermaid
sequenceDiagram
    participant Browser
    participant Controller
    participant AppService
    participant DomainService
    participant Repository
    participant DbContext
    participant Database
    
    Browser->>Controller: HTTP Request (POST /applications)
    Controller->>Controller: Model Binding & Validation
    Controller->>AppService: CreateApplicationAsync(dto)
    
    Note over AppService: Authorization Check<br/>[Authorize] Attribute
    Note over AppService: Start Unit of Work<br/>Begin Transaction
    
    AppService->>AppService: Map DTO to Domain Entity
    AppService->>DomainService: ValidateApplicationRules(entity)
    DomainService-->>AppService: Validation Result
    
    AppService->>Repository: InsertAsync(entity)
    Repository->>DbContext: Add(entity)
    
    Note over AppService: Publish Domain Event<br/>ApplicationCreatedEvent
    
    AppService->>AppService: Commit Unit of Work
    DbContext->>Database: INSERT Application
    Database-->>DbContext: Success
    
    Note over AppService: Distributed Event<br/>Published to RabbitMQ
    
    AppService->>AppService: Map Entity to DTO
    AppService-->>Controller: ApplicationDto
    Controller-->>Browser: HTTP 200 + JSON Response
```

### Cross-Cutting Concerns (Automatic via ABP)

ABP Framework automatically handles the following concerns for application services:

1. **Authorization**: `[Authorize]` attributes and permission checks via `IAuthorizationService`
2. **Validation**: Automatic input DTO validation using data annotations and FluentValidation
3. **Unit of Work**: Automatic transaction management with commit/rollback
4. **Audit Logging**: Automatic logging of method calls, parameters, and results
5. **Exception Handling**: Global exception filter with appropriate HTTP status codes
6. **Multi-Tenancy**: Automatic tenant resolution and data isolation

## Database Schema Strategy

### Multi-Database Approach

```mermaid
erDiagram
    HOST_DB ||--o{ TENANTS : contains
    HOST_DB ||--o{ USERS : contains
    HOST_DB ||--o{ ROLES : contains
    HOST_DB ||--o{ SETTINGS : contains
    
    TENANT_DB ||--o{ GRANT_PROGRAMS : contains
    TENANT_DB ||--o{ APPLICATIONS : contains
    TENANT_DB ||--o{ ASSESSMENTS : contains
    TENANT_DB ||--o{ PAYMENTS : contains
    TENANT_DB ||--o{ DOCUMENTS : contains
    
    GRANT_PROGRAMS ||--o{ APPLICATIONS : has
    APPLICATIONS ||--o{ ASSESSMENTS : has
    APPLICATIONS ||--o{ PAYMENTS : receives
    APPLICATIONS ||--o{ DOCUMENTS : includes
```

**Host Database (`GrantManagerDbContext`):**
- Tenant definitions and configurations
- Users and roles (cross-tenant identity)
- Global settings and feature flags
- Audit logs
- Background job definitions

**Tenant Databases (`GrantTenantDbContext`):**
- Grant programs and configurations
- Applications and applicant data
- Assessment workflows and scores
- Payment requests and history
- Documents and attachments
- Tenant-specific settings

### Migration Strategy

1. **Host Migrations**: Located in `Unity.GrantManager.EntityFrameworkCore/Migrations/`
   ```bash
   dotnet ef migrations add <MigrationName> --context GrantManagerDbContext
   ```

2. **Tenant Migrations**: Located in `Unity.GrantManager.EntityFrameworkCore/TenantMigrations/`
   ```bash
   dotnet ef migrations add <MigrationName> --context GrantTenantDbContext
   ```

3. **DbMigrator**: Console application that applies both host and tenant migrations on startup

## Deployment Architecture

### Development Environment

- **Single Instance**: All modules hosted in single ASP.NET Core process
- **Database**: Local PostgreSQL instance (can be Docker container)
- **Redis**: Local Redis instance (optional, uses in-memory cache as fallback)
- **RabbitMQ**: Local RabbitMQ instance (can be disabled for development)

### Production Environment (Modular Monolith)

```mermaid
graph TB
    subgraph "Load Balancer (nginx)"
        LB[nginx<br/>Round-robin]
    end
    
    subgraph "Web Application (3 replicas)"
        Web1[Unity.GrantManager.Web<br/>Instance 1]
        Web2[Unity.GrantManager.Web<br/>Instance 2]
        Web3[Unity.GrantManager.Web<br/>Instance 3]
    end
    
    subgraph "Data Layer"
        PG[(PostgreSQL<br/>Host + Tenant DBs)]
        Redis[(Redis<br/>Cache + Sessions)]
        S3[(Common Object Management Service<br/>Blob Storage)]
    end
    
    subgraph "Message Broker"
        RabbitMQ[RabbitMQ<br/>Event Bus]
    end
    
    subgraph "External Services"
        Keycloak[Keycloak<br/>Identity Provider]
        CHES[CHES<br/>Email Service]
        CAS[CAS<br/>Payment System]
    end
    
    LB --> Web1
    LB --> Web2
    LB --> Web3
    
    Web1 --> PG
    Web2 --> PG
    Web3 --> PG
    
    Web1 --> Redis
    Web2 --> Redis
    Web3 --> Redis
    
    Web1 --> S3
    Web2 --> S3
    Web3 --> S3
    
    Web1 --> RabbitMQ
    Web2 --> RabbitMQ
    Web3 --> RabbitMQ
    
    Web1 --> Keycloak
    Web2 --> Keycloak
    Web3 --> Keycloak
    
    Web1 --> CHES
    Web2 --> CHES
    Web3 --> CHES
    
    Web1 --> CAS
    Web2 --> CAS
    Web3 --> CAS
```

**Configuration:**
- Load balancer distributes requests across 3 web instances (Docker Compose with nginx)
- Redis used for distributed caching and session storage
- RabbitMQ provides reliable message delivery between instances
- PostgreSQL handles both host and multiple tenant databases
- Background jobs coordinated via Quartz.NET clustering

## Security Architecture

### Authentication Flow

```mermaid
sequenceDiagram
    participant User
    participant WebApp
    participant Keycloak
    participant Database
    
    User->>WebApp: Access Protected Page
    WebApp->>WebApp: Check Authentication
    WebApp-->>User: Redirect to Login
    User->>Keycloak: Login (username/password)
    Keycloak->>Keycloak: Validate Credentials
    Keycloak-->>User: Redirect with Auth Code
    User->>WebApp: Auth Code
    WebApp->>Keycloak: Exchange Code for Token
    Keycloak-->>WebApp: ID Token + Access Token
    WebApp->>WebApp: Validate Token & Create Session
    WebApp->>Database: Load User Permissions
    Database-->>WebApp: Roles & Permissions
    WebApp-->>User: Redirect to Requested Page
```

### Authorization Model

- **Role-Based Access Control (RBAC)**: Roles assigned to users (Admin, ProgramOfficer, Assessor, Applicant)
- **Permission-Based**: Granular permissions checked via `[Authorize]` attributes and `IAuthorizationService`
- **Multi-Tenant Isolation**: Tenant context automatically applied to all queries and operations
- **Data-Level Security**: Row-level security via ABP's data filters and tenant resolution

## Performance & Scalability Considerations

### Caching Strategy
- **Distributed Cache (Redis)**: Application settings, user permissions, frequently accessed lookup data
- **Local Memory Cache**: Static configuration, short-lived data
- **HTTP Response Caching**: Public pages and API responses with ETags

### Database Optimization
- **Indexing**: Strategic indexes on foreign keys, tenant IDs, and frequently queried fields
- **Query Optimization**: `IQueryable` projections to load only required fields
- **Eager Loading**: Configured includes to avoid N+1 query problems
- **Async Operations**: All database operations use async/await pattern

### Background Processing
- **Quartz.NET Jobs**: Long-running tasks (report generation, payment processing, email sending)
- **Clustering**: Background jobs coordinated across multiple instances
- **Event-Driven**: Asynchronous processing via RabbitMQ distributed events

### Scalability
- **Horizontal Scaling**: Stateless web instances can be added behind load balancer
- **Database Partitioning**: Separate tenant databases enable independent scaling
- **Blob Storage**: Large files stored in COMS, not in database
- **CDN Ready**: Static assets can be served from CDN

## References

- [ABP Framework Documentation](https://docs.abp.io/en/abp/latest)
- [ABP Domain Driven Design](https://docs.abp.io/en/abp/latest/Domain-Driven-Design)
- [ABP Multi-Tenancy](https://docs.abp.io/en/abp/latest/Multi-Tenancy)
- [ABP Module Architecture Best Practices](https://docs.abp.io/en/abp/latest/Best-Practices/Module-Architecture)
- [Implementing Domain Driven Design (e-book)](https://abp.io/books/implementing-domain-driven-design)
