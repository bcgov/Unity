# Tenant Management Application Services

## `ITenantAppService` / `TenantAppService`

`Unity.TenantManagement.Application/TenantAppService.cs`, class-level `[Authorize(TenantManagementPermissions.Policies.TenantsOrITOps)]` (list/read access baseline), individual methods tightened further per-action:

|Method|Policy|Notes|
|---|---|---|
|`GetAsync(id)`|`TenantsOrITOps`|Maps `Tenant` → `TenantDto`.|
|`GetListAsync(GetTenantsInput)`|`TenantsOrITOps`|Two query paths — see below.|
|`CreateAsync(TenantCreateDto)`|`TenantsCreateOrITOps`|Full tenant provisioning — see below.|
|`UpdateAsync(id, TenantUpdateDto)`|`TenantsUpdateOrITOps`|Renames + updates `ExtraProperties`; `SetConcurrencyStampIfNotNull`.|
|`DeleteAsync(id)`|`Tenants.Delete`|**Deletes only the ABP `Tenant` row.** Does not drop the Postgres database/roles or deregister anything in Metabase — see [tenant-management-roadmap.md](tenant-management-roadmap.md).|
|`GetCurrentTenantCasClientCodeAsync(tenantId)`|`[AllowAnonymous]`, `[RemoteService(false)]`|Internal-only helper (e.g. for payment flows needing the current tenant's CAS code without a full tenant read).|
|`GetCurrentTenantName()`|(inherited)|Reads `ICurrentTenant.GetId()` and looks the name up.|
|`AssignManagerAsync(TenantAssignManagerDto)`|`TenantsUpdateOrITOps`|Publishes `TenantAssignManagerEto` (local event) rather than assigning directly — decoupled the same way Flex decouples from the host.|
|`GetConnectionStringsAsync(id)` / `UpdateConnectionStringsAsync(id, dto)`|`Tenants.ManageConnectionStrings`|Decrypts for display / encrypts on save. `UpdateConnectionStringsAsync` **only rewrites the encrypted DB row — it does not touch Postgres**, which is the root of the password-desync gap noted in [tenant-management-domain-model.md](tenant-management-domain-model.md#postgres-role-provisioning).|
|`GetManagersAsync(id)`|`TenantsUpdateOrITOps`|Switches into the tenant (`ICurrentTenant.Change(id)`) and lists users with the `PROGRAM_MANAGER` normalized role.|

### `GetListAsync` — two query paths

`GetTenantsInput` supports filtering/sorting on both native `Tenant` columns and the `ExtraProperties`-backed fields (`DisplayName`, `Division`, `Branch`, `Description`, `CasClientCode`). Because `ExtraProperties` isn't a real relational column, the method branches:

- **Fast DB path** — used when sorting/filtering only touches native columns (`Id`, `Name`, `NormalizedName`, `CreationTime`, `LastModificationTime`). Delegates straight to `ITenantRepository.GetListAsync`/`GetCountAsync`.
- **In-memory path** — used the moment an extra-property field is involved in sorting or any filter is present. Fetches the **full unfiltered** tenant list (repository-level filtering only matches `Name`, which would silently exclude an extra-property-only match), then applies filter/sort/paging in memory via `MatchesExtraProperty`/`GetExtraPropertyValue` helpers.

Deliberate tradeoff for tenant-count scale (dozens to low hundreds), not thousands — acceptable today, worth revisiting if the tenant list grows substantially.

### `CreateAsync` — full provisioning sequence

Inside one unit of work:

1. `StripPrivilegedFieldsUnlessAuthorized(input, callerIsAuthorized)` — re-checks `IdentityConsts.ITAdminOrITOperationsPolicyName` server-side and nulls `FeatureKeys`/`MetabaseUserEmails` if the caller isn't IT Admin/Ops, even though a caller reaching this method already holds `TenantsCreateOrITOps` — see [tenant-management-domain-model.md](tenant-management-domain-model.md#roleorpermissionrequirement--the-actual-or-logic) for why this second check exists.
2. `tenantManager.CreateAsync(input.Name)`.
3. Generates and encrypts both connection strings (`TenantConnectionStringBuilder` — see domain model doc), attaches as `TenantConnectionString` rows named `"Tenant"`/`"Tenant_Readonly"`.
4. Sets `ExtraProperties` (`LicencePlate`, `DisplayName`, `Division`, `Branch`, `Description`, `CasClientCode`).
5. `tenant.SeedPostTenantCreationSections(postTenantCreationSteps)` — seeds a `Waiting` status for every registered `IPostTenantCreationStep`, injected as `IEnumerable<IPostTenantCreationStep>` (resolved across the whole app, including steps implemented in `Unity.GrantManager.Application` — DI doesn't care about compile-time project references). See [tenant-management-post-creation.md](tenant-management-post-creation.md).
6. Inserts, commits the UoW.
7. **After** the UoW commits, publishes `TenantCreatedEto` (`UserIdentifier`, `FeatureKeys`, optionally `MetabaseUserEmails`) — picked up by `Unity.GrantManager.Application`'s `TenantCreatedEventHandler`, which synchronously migrates/seeds the tenant DB and Postgres roles (see domain model doc), imports the initial IDIR user as Program Manager, enables requested features, snapshots the Metabase user-email setting, and finally enqueues the deferred post-creation job sequence.

`CreateAsync` is the single choke point both the "New Tenant" modal **and** Onboarding approval (`OnboardingRequestAppService.CreateTenantAsync`) call — see [tenant-management-onboarding.md](tenant-management-onboarding.md).

## `IOnboardingRequestAppService` / `OnboardingRequestAppService`

Class-level `[Authorize(IdentityConsts.ITOperationsPolicyName)]` — the entire onboarding surface is IT-Operations-only (IT Administrators use the direct "New Tenant" form instead). Full lifecycle covered in [tenant-management-onboarding.md](tenant-management-onboarding.md); API surface summary:

|Method|Purpose|
|---|---|
|`GetListAsync(OnboardingListRequestDto)`|Paged, filtered, sorted list merging core `Application` fields with mapped Flex worksheet fields.|
|`GetAsync(id)`|One onboarding request, fully resolved (core fields + mapped worksheet fields).|
|`ValidateAsync(id)`|Client-triggered pre-check — runs the same validation steps `CreateTenantAsync` re-runs server-side.|
|`CreateTenantAsync(id, CreateTenantInputDto?)`|Approves the request: resolves super users, feature keys, calls `TenantAppService.CreateAsync`, assigns extra managers, updates the Metabase default email list, closes the source application.|
|`GetColumnSchemaAsync()` / `GetAvailableCategoriesAsync()`|Drives the admin field-mapping UI (which worksheet field key means "tenant name", etc.) and the intake-category picker.|

Exposed over HTTP by a **hand-written** `OnboardingRequestController` (`api/onboarding-requests`) rather than an auto-generated ABP dynamic proxy — every action additionally does a defensive `ModelState.IsValid` check throwing `UserFriendlyException`, redundant with ASP.NET Core's own model validation but consistent across all five routes.

## Endpoint management — configurable third-party API base URLs

UI lives in this module (`Unity.TenantManagement.Web/Pages/EndpointManagement/Endpoints/`), but the backing service and entity are **host-owned**:

- `EndpointManagementAppService` (`src/Unity.GrantManager.Application/Integrations/Endpoints/`) — a stock ABP `CrudAppService<DynamicUrl,...>`.
- `DynamicUrl` entity (oddly, defined in `Unity.Notifications.Domain/Settings/DynamicUrl.cs`, not TenantManagement or GrantManager) — `KeyName`, `Url`, `Description`, nullable `TenantId`.

The schema **supports** per-tenant URL overrides via the nullable `TenantId`, but every actual consumer found (`GetGitHubRepoUrlAsync`, `GetChefsApiBaseUrlAsync`, and `MetabaseApiClient.GetBaseUrlAsync` via `GetUgmUrlByKeyNameAsync`) calls the `tenantSpecific: false` path — so in practice, today, endpoints like the Metabase API base URL are **global-only** (`TenantId == null` rows) despite the per-tenant capability existing unused in the schema. Results are cached in `IDistributedCache` for 1 hour with a tracked-key-set mechanism for bulk invalidation on update.

## CAS client code lookup

`ICasClientCodeLookupService` (consumed by the Tenants Create/Edit/Configuration modals for a dropdown when setting a tenant's `CasClientCode` extra property) sits in front of the **host-owned** `CasClientCode` entity (`src/Unity.GrantManager.Domain.Shared/Integrations/CasClientCode.cs` — `FullAuditedAggregateRoot<Guid>`: `ClientCode` (max 3 chars), `Description`, `MinistryPrefix`, `FinancialMinistry`, `ClientId`, `IsActive`). This module's role is purely "let an admin attach the right code to a tenant" — the actual CAS integration logic (`CasTokenService`, `InvoiceService`, `SupplierService` under `Unity.Payments`) lives entirely outside TenantManagement; it just reads the code back off the tenant later for payment/invoicing.

## Metabase settings

`MetabaseSettings.UserEmails` (`Application.Contracts/Metabase/MetabaseSettings.cs` + `MetabaseSettingDefinitionProvider`) — a comma-separated default email list, settable **globally** and **per-tenant** (`TenantSettingValueProvider`). Two write paths:

- `TenantCreatedEventHandler.SaveMetabaseUserEmailsAsync` snapshots the resolved list into a **per-tenant** setting at creation time, so the later, asynchronous Metabase registration step reads a stable list even if the global default changes in the meantime.
- `OnboardingRequestAppService`'s Metabase handling (`UpdateMetabaseDefaultUserEmailsAsync`) folds newly-added/removed emails into the **global** default list when an operator checks "save as default" while approving an onboarding request — the same mechanism the "New Tenant" modal's Metabase tab exposes.

See [tenant-management-post-creation.md](tenant-management-post-creation.md) for how this setting is actually consumed by the Metabase registration step.
