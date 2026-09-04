# Unity.TenantManagement Module Documentation

Unity.TenantManagement is the application/web layer for **creating, provisioning, and configuring tenants** in Unity Portal — from IT Administrators using a direct "New Tenant" form, through IT Operations reviewing and approving **Onboarding** requests submitted as a specialized CHEFS intake form, to ongoing per-tenant configuration (connection strings, features, Metabase reporting access, CAS client code, managers). Unlike most other modules in this codebase, it owns no domain layer or database schema of its own — it's a thin skin over ABP's built-in tenant-management module plus host-owned supporting data, resolved entirely at runtime through DI.

This folder documents how the module is built and how it is used. Read in this order:

1. **[tenant-management-overview.md](tenant-management-overview.md)** — what problem it solves, the "no domain layer of its own" architecture, dependency direction, core concepts glossary.
2. **[tenant-management-domain-model.md](tenant-management-domain-model.md)** — the `Tenant` entity and its `ExtraProperties`, connection-string encryption, Postgres role provisioning, the IT Administrator/IT Operations permission and policy layer.
3. **[tenant-management-application-services.md](tenant-management-application-services.md)** — `TenantAppService` and `OnboardingRequestAppService` APIs, endpoint management, CAS client code lookup, Metabase settings.
4. **[tenant-management-onboarding.md](tenant-management-onboarding.md)** — how Onboarding turns a submitted intake form into a fully provisioned tenant: field mapping, validation steps, user lookup, and the approval flow.
5. **[tenant-management-post-creation.md](tenant-management-post-creation.md)** — the deferred post-tenant-creation job sequence (Metabase registration today), its status tracking, and how the Metabase API client handles that API's inconsistencies.
6. **[tenant-management-web-ui.md](tenant-management-web-ui.md)** — full Razor Pages inventory and the permission gating on each page.
7. **[tenant-management-roadmap.md](tenant-management-roadmap.md)** — known rough edges: connection-string password desync risks, orphaned resources on tenant deletion, and other gaps worth knowing before extending this module.

## Source location

```
applications/Unity.GrantManager/modules/Unity.TenantManagement/
├── src/
│   ├── Unity.TenantManagement.Application.Contracts/   DTOs, app service interfaces
│   ├── Unity.TenantManagement.Application/              TenantAppService, OnboardingRequestAppService, validation
│   │                                                     steps, connection string builder, Metabase settings
│   ├── Unity.TenantManagement.Web/                      Razor Pages: Tenants/, Onboarding/, EndpointManagement/,
│   │                                                     Reconciliation/
│   ├── Unity.TenantManagement.HttpApi/                  Controllers
│   └── Unity.TenantManagement.HttpApi.Client/           Generated client proxy
└── test/
    ├── Unity.TenantManagement.TestBase/
    ├── Unity.TenantManagement.Application.Tests/
    └── Unity.TenantManagement.EntityFrameworkCore.Tests/   (exercises ABP's own TenantManagementDbContext)
```

No `Unity.TenantManagement.Domain` or `Unity.TenantManagement.EntityFrameworkCore` project exists — see [tenant-management-overview.md](tenant-management-overview.md#the-one-big-architectural-fact) for why.

Host-side implementations of this module's contracts live in `applications/Unity.GrantManager/src/Unity.GrantManager.Application/TenantManagement/` (`GrantManagerOnboardingApplicationProvider.cs`, `OnboardingCoreFieldRegistry.cs`) and `.../Identity/CssOnboardingUserLookup.cs`. Postgres/EF provisioning is in `Unity.GrantManager.EntityFrameworkCore/EntityFrameworkCore/EntityFrameworkCoreGrantManagerDbSchemaMigrator.cs`. The post-tenant-creation job sequence spans `Unity.GrantManager.Application/Tenants/PostCreation/` and `modules/Unity.SharedKernel/PostTenantCreation/`.

A related one-page visual summary is in `documentation/handover/tenant-management-handover.html`.
