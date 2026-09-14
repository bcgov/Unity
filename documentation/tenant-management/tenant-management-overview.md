# Tenant Management Overview

## What problem it solves

Unity Portal is multi-tenant: every ministry/program area that uses the system is a separate ABP tenant with its own database, its own users, its own feature set. `Unity.TenantManagement` is the module that lets IT Administrators and IT Operations **create, configure, and onboard** those tenants — from a raw "New Tenant" form for IT Admins, through a guided **Onboarding** approval flow for IT Operations that turns a submitted intake form into a fully provisioned tenant, to ongoing per-tenant configuration (connection strings, features, Metabase reporting access, CAS client code, managers).

## The one big architectural fact

Unlike `Unity.Flex` (which owns its entire domain model, EF Core context, and database schema), **`Unity.TenantManagement` owns no domain layer and no persistence of its own.** There is no `Unity.TenantManagement.Domain` or `Unity.TenantManagement.EntityFrameworkCore` project. Everything it manages is one of:

- **ABP's own built-in `Tenant`/`TenantConnectionString` entities** (`Volo.Abp.TenantManagement`), extended purely through `ExtraProperties` (flat string keys) rather than a custom entity — hosted in the **host** `GrantManagerDbContext` via `[ReplaceDbContext(typeof(ITenantManagementDbContext))]`.
- **Host-owned entities it never compiles against**, resolved purely via runtime DI — `CasClientCode`, `DynamicUrl`, `Application`/`WorksheetInstance` (for onboarding) — all defined in `Unity.GrantManager.*` or other modules.

This module is a thin **Application + Web layer** wrapping (a) ABP's stock tenant-management module and (b) host-owned supporting data it discovers at runtime. This inversion — TenantManagement defines the contracts, `Unity.GrantManager.Application` implements the host-specific pieces (`GrantManagerOnboardingApplicationProvider`, `CssOnboardingUserLookup`) — is the same decoupling idea Flex uses for its local-event handlers, just wired through plain DI resolution instead of an event bus.

## Module layout and dependency direction

```text
Unity.TenantManagement.Application.Contracts   → DTOs, app service interfaces (AbpDddApplicationContractsModule)
        ↑
Unity.TenantManagement.Application             → AbpTenantManagementDomainModule (ABP's own), Unity.SharedKernel,
                                                  Unity.Flex.Application.Contracts
                                                  ZERO reference to Unity.GrantManager — host callbacks resolved via DI only
        ↑
Unity.TenantManagement.Web                     → Razor Pages/modals. Reaches into Unity.GrantManager.Application.Contracts
                                                  and Unity.Reporting.Application.Contracts for UI convenience — the one
                                                  place this module's strict "no host reference" rule is relaxed, and only
                                                  at the Web tier
        ↑
Unity.TenantManagement.HttpApi / .HttpApi.Client → thin controller/proxy wrappers around the Application layer's contracts
```

`Unity.GrantManager.Application` implements two contracts TenantManagement defines but cannot itself fulfil, resolved lazily via `LazyServiceProvider.LazyGetService<T>()` (degrading gracefully to empty results if nothing is registered, rather than throwing):

- `IOnboardingApplicationProvider` → `GrantManagerOnboardingApplicationProvider` — reads onboarding request data out of the host's `Application`/Flex `WorksheetInstance` tables (see [tenant-management-onboarding.md](tenant-management-onboarding.md)).
- `IOnboardingUserLookup` → `CssOnboardingUserLookup` — resolves an email to an IDIR GUID via the host's existing CSS/IDIR directory integration.

## Core concepts (glossary)

|Term|Meaning|
|---|---|
|**Tenant**|Stock ABP `Volo.Abp.TenantManagement.Tenant` — one ministry/program area's isolated database + user base. Extended via flat `ExtraProperties` (`DisplayName`, `Division`, `Branch`, `Description`, `CasClientCode`, `LicencePlate`, plus post-creation step status — see below).|
|**Licence plate**|The auto-generated `T_XXX999` (3 random letters + 3 random digits) tenant/database/username stem, stored as the `LicencePlate` extra property.|
|**Tenant / Tenant_Readonly connection strings**|Two AES-256-encrypted `TenantConnectionString` rows per tenant — a full read-write role and an independent read-only role, both Postgres roles provisioned at tenant-creation time.|
|**Onboarding request**|Not a dedicated entity — a `Unity.GrantManager` `Application` whose form's category is `"Onboarding"`, submitted through the normal CHEFS intake pipeline and read back through `IOnboardingApplicationProvider`. See [tenant-management-onboarding.md](tenant-management-onboarding.md).|
|**Post-tenant-creation step**|One unit of asynchronous provisioning work run after a tenant is created (currently just Metabase registration), tracked via `IPostTenantCreationStep` and surfaced in the Tenants list UI. See [tenant-management-post-creation.md](tenant-management-post-creation.md).|
|**IT Administrator / IT Operations**|Two distinct Keycloak roles with overlapping-but-different tenant-management rights — see [tenant-management-domain-model.md](tenant-management-domain-model.md#permissions-and-policies). Roughly: IT Admin can do anything directly; IT Operations creates tenants only through Onboarding approval.|
|**CAS client code**|BC Government's Corporate Accounting System ministry client code, attached to a tenant for downstream `Unity.Payments` invoicing/reconciliation.|

## Read in this order

1. **[tenant-management-overview.md](tenant-management-overview.md)** (this file) — problem, architecture, glossary.
2. **[tenant-management-domain-model.md](tenant-management-domain-model.md)** — the `Tenant` entity, connection strings and their encryption, Postgres role provisioning, permissions/policies.
3. **[tenant-management-application-services.md](tenant-management-application-services.md)** — `TenantAppService`, endpoint management, CAS client code lookup, Metabase settings.
4. **[tenant-management-onboarding.md](tenant-management-onboarding.md)** — the guided, IT-Operations tenant-creation flow built on top of the intake/Flex pipeline.
5. **[tenant-management-post-creation.md](tenant-management-post-creation.md)** — the deferred post-creation job sequence (Metabase registration) and its status-tracking UI.
6. **[tenant-management-web-ui.md](tenant-management-web-ui.md)** — full Razor Pages inventory and permission gating per page.
7. **[tenant-management-roadmap.md](tenant-management-roadmap.md)** — known rough edges and unfinished capabilities worth knowing about before extending this module.

## Source location

```
applications/Unity.GrantManager/modules/Unity.TenantManagement/
├── src/
│   ├── Unity.TenantManagement.Application.Contracts/   DTOs, app service interfaces
│   ├── Unity.TenantManagement.Application/              TenantAppService, OnboardingRequestAppService, validation steps,
│   │                                                     connection string builder, Metabase settings
│   ├── Unity.TenantManagement.Web/                      Razor Pages: Tenants/, Onboarding/, EndpointManagement/, Reconciliation/
│   ├── Unity.TenantManagement.HttpApi/                  Controllers
│   └── Unity.TenantManagement.HttpApi.Client/           Generated client proxy
└── test/
    ├── Unity.TenantManagement.TestBase/
    ├── Unity.TenantManagement.Application.Tests/
    └── Unity.TenantManagement.EntityFrameworkCore.Tests/  (tests ABP's own TenantManagementDbContext, not a module-owned one)
```

Host-side implementation of TenantManagement's contracts lives in `applications/Unity.GrantManager/src/Unity.GrantManager.Application/TenantManagement/` (`GrantManagerOnboardingApplicationProvider.cs`, `OnboardingCoreFieldRegistry.cs`) and `.../Identity/CssOnboardingUserLookup.cs`. Postgres/EF provisioning lives in `Unity.GrantManager.EntityFrameworkCore/EntityFrameworkCore/EntityFrameworkCoreGrantManagerDbSchemaMigrator.cs`. The post-tenant-creation job sequence lives in `Unity.GrantManager.Application/Tenants/PostCreation/` and `modules/Unity.SharedKernel/PostTenantCreation/`.

A related one-page visual summary is in `documentation/handover/tenant-management-handover.html`.
