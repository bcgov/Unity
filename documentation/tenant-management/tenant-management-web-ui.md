# Tenant Management Web UI

All Razor Pages live under `modules/Unity.TenantManagement/src/Unity.TenantManagement.Web/Pages/`, registered via `RazorPagesOptions.Conventions.AuthorizePage` in `UnityTenantManagementWebModule.cs` — page-level authorization, not just toolbar-button visibility (see the note on IT Operations below).

## `TenantManagement/Tenants/` — tenant CRUD and configuration

|Page|Purpose|Authorization|
|---|---|---|
|`Index.cshtml(.cs)`|The Tenants admin list — DataTable with Name, Display Name, Licence Plate, Division, Branch, Description, CAS Client Code, and the post-creation "Setup Status" icon (see [tenant-management-post-creation.md](tenant-management-post-creation.md)).|`TenantsOrITOps`|
|`CreateModal.cshtml(.cs)`|"New Tenant" form — name, display metadata, CAS client code, and (IT-Admin/Ops only) Features and Metabase tabs.|`ITAdministrator` role only — tightened deliberately so IT Operations creates tenants through Onboarding instead (see below).|
|`EditModal.cshtml(.cs)`|Rename + edit `ExtraProperties` (display metadata, CAS code) for an existing tenant.|`Tenants.Update` (resolves to IT Admin/Ops OR the raw permission — see domain model doc)|
|`ConfigurationModal.cshtml(.cs)`|Everything else for an existing tenant: connection-string management, Features/Metabase tabs, the Reporting view-role tab (`Unity.Reporting.Application.Contracts`), and the managers list.|`TenantsOrITOps`, with `ManageConnectionStrings`/`ITAdminOrITOperations` gating individual tabs|
|`AssignManagerModal.cshtml(.cs)`|Attach an additional program manager to an existing tenant (separate control from Onboarding's "extra super users" loop, though both ultimately call `TenantAppService.AssignManagerAsync`).|`Tenants.Create` (shared with the create-flow policy)|
|`TenantManagementPageModel`|Shared abstract base for this folder.|—|

### Why "New Tenant" is IT-Administrator-only

Both `TenantAppService.CreateAsync` (the app service) and `OnboardingRequestAppService.CreateTenantAsync` (the onboarding approval path) are authorized under the same `TenantsCreateOrITOps` policy — which resolves to "IT Administrator OR IT Operations" (see [tenant-management-domain-model.md](tenant-management-domain-model.md#roleorpermissionrequirement--the-actual-or-logic)). If the "New Tenant" toolbar button and its page were left on that same broad policy, IT Operations could create tenants directly, bypassing the reviewed, validated Onboarding queue entirely. Two changes close this, both scoped to `CreateModal` only (not the underlying `TenantAppService.CreateAsync`, which Onboarding still needs):

- The toolbar button's `AbpPageToolbarOptions.requiredPolicyName` (`UnityTenantManagementWebModule.cs`) is `IdentityConsts.ITAdminPolicyName` — a plain role check, not the OR-composite policy.
- The `/TenantManagement/Tenants/CreateModal` page's own `AuthorizePage` convention uses the same `IdentityConsts.ITAdminPolicyName` — closing the "navigate straight to the URL, bypassing the hidden button" gap that hiding the button alone would have left open.

IT Operations still sees the Tenants list (`TenantsOrITOps`) and can still create tenants — through `TenantManagement/Onboarding/` instead. See [tenant-management-onboarding.md](tenant-management-onboarding.md#why-it-operations-specifically).

## `TenantManagement/Onboarding/` — see [tenant-management-onboarding.md](tenant-management-onboarding.md#web-ui)

Both pages `[Authorize(IdentityConsts.ITOperationsPolicyName)]`: `Index.cshtml(.cs)` (request queue) and `CreateTenantModal.cshtml(.cs)` (approval).

## `TenantManagement/Reconciliation/`

`Index.cshtml(.cs)` — a CHEFS-vs-Unity submission-count reconciliation/audit tool (date range, tenant, category filters; a "Missing Submissions Browser" DataTable). Adjacent IT-ops tooling filed under this module's navigation rather than tenant CRUD proper. Authorization is a plain `[Authorize]` with **no specific policy** — worth flagging as inconsistent with the rest of the module's explicit IT-role/permission gating, though not necessarily wrong (any authenticated user reaching this page still needs whatever backs the underlying data query).

## `EndpointManagement/Endpoints/`

`Index.cshtml.cs` (a near-empty `PageModel` — not even this module's own `TenantManagementPageModel` base), `CreateModal.cshtml.cs`, `UpdateModal.cshtml.cs`, `EndpointManagementPageModel.cs`. CRUD UI for the `DynamicUrl` entity — see [tenant-management-application-services.md](tenant-management-application-services.md#endpoint-management--configurable-third-party-api-base-urls).

## HTTP surface (`Unity.TenantManagement.HttpApi`)

- `TenantController` — mirrors `ITenantAppService`.
- `OnboardingRequestController` — hand-written mirror of `IOnboardingRequestAppService` at `api/onboarding-requests` (`GET /`, `GET /{id}`, `GET /{id}/validate`, `POST /{id}/create-tenant`, `GET /column-schema`, `GET /categories`) rather than an auto-generated ABP dynamic proxy.
- `UnityTenantManagementHttpApiModule`.

`Unity.TenantManagement.HttpApi.Client` generates a `TenantClientProxy` for machine-to-machine consumption — not covered further here; low relevance to the day-to-day admin UI.
