# Onboarding — Tenant Creation as a Specialized Intake Form

Onboarding is IT Operations' path to creating a tenant: instead of IT Administrators hand-filling the "New Tenant" modal, a requester submits an ordinary CHEFS intake form, and IT Operations reviews and approves it from a request queue, which provisions the tenant using data pulled straight out of that submission. It reuses the existing intake/Flex pipeline wholesale rather than being a separate feature — this is the single most important thing to understand about it.

## The core idea: an onboarding request is not a real entity

There is no `OnboardingRequest` table. An **onboarding request is a `Unity.GrantManager` `Application`** (the exact same entity type behind ordinary grant applications) whose `ApplicationForm.Category == "Onboarding"`. A CHEFS form is authored with that category, submitted through the normal intake pipeline, and the resulting `Application` — plus its Flex `WorksheetInstance` data (arbitrary JSONB custom fields) — is what the Onboarding screens read and act on. Zero new persistence was built for this feature; it's the intake/Flex machinery, repurposed.

## The seam: `IOnboardingApplicationProvider`

Defined in `Unity.TenantManagement.Application.Contracts`, implemented in the **host**:

```csharp
GrantManagerOnboardingApplicationProvider   // src/Unity.GrantManager.Application/TenantManagement/
    [ExposeServices(typeof(IOnboardingApplicationProvider))]
```

`GetPagedListAsync`, `GetByIdAsync`, `GetAllIdsAsync`, `GetFormVersionIdsAsync`, `GetMappedCoreFieldColumnsAsync`, `GetAvailableCategoriesAsync`, `CloseApplicationAsync`. `Unity.TenantManagement.Application` has **zero compile-time reference to `Unity.GrantManager`** — confirmed via its `.csproj` (only `AbpTenantManagementDomainModule`, `Unity.SharedKernel`, `Unity.Flex.Application.Contracts`). `OnboardingRequestAppService` resolves the provider via `LazyServiceProvider.LazyGetService<IOnboardingApplicationProvider>()` and degrades gracefully to empty results if nothing is registered — the same runtime-DI inversion pattern used for `IOnboardingUserLookup` (below) and, structurally, for Flex's local-event handlers.

## Two data sources merged per request

An onboarding request's visible fields come from two places, merged in `OnboardingRequestAppService`:

1. **Core fields** — real `Application`/`Applicant`/`ApplicantAgent` columns (`ProjectName`, `RequestedAmount`, `ApplicantName`, `SigningAuthorityEmail`, contact fields, etc.), enumerated by `OnboardingCoreFieldRegistry` (`src/Unity.GrantManager.Application/TenantManagement/OnboardingCoreFieldRegistry.cs`) — a static list of `CoreFieldDefinition(Key, Label, Type, EfPath, Selector)` records. `EfPath` lets these be filtered/sorted **in SQL** via `System.Linq.Dynamic.Core`. Only fields the registry confirms are actually persisted by intake are listed; several theoretically-mappable `IntakeMapping` fields are deliberately left out.
2. **Worksheet fields** — arbitrary Flex `CustomField`s attached to the onboarding form's `WorksheetInstance`s, fetched via `IWorksheetInstanceAppService.GetListByCorrelationIdsAsync` and parsed **in memory**, since JSONB values can't be filtered/sorted in SQL. `OnboardingRequestAppService.GetListAsync` carries an explicit PERF comment flagging this as fine at current tenant/request volume, worth revisiting if the screen scales.

## Field mapping is admin-configurable, not hardcoded

Which worksheet-field *key* means "tenant name", "display name", "super users", "branch", "features", "ministry", "division", "program area" is **not** fixed in code — it's stored per-user (falling back to a global default) via `ISettingManager` (`OnboardingColumnConfigSettings`, provider `"U"`). `ReadTenantMappingAsync` / `SaveFieldMappingAsync` / `ResolveFieldMappings` implement this. Practical effect: different onboarding CHEFS forms — with entirely different field keys — can all feed the same tenant-creation flow, as long as an admin maps the relevant columns once through the UI. This is what makes "one onboarding pipeline, many possible intake forms" work without a code change per form revision.

## Validation steps (`IOnboardingValidationStep`)

Same auto-discovery pattern as post-creation steps: `ITransientDependency`, `[RemoteService(false)]`, `Order`/`StepName`/`ValidateAsync(OnboardingRequestDto) → OnboardingValidationStepResult`, run in ascending order via `RunValidationStepsAsync`, collecting `"[{StepName}] {Issue}"` messages.

|Step|Order|Checks|
|---|---|---|
|`TenantNameUniquenessStep`|10|`ITenantRepository.FindByNameAsync(name.ToUpper())` — no existing tenant with this normalized name.|
|`SuperUsersValidationStep`|20|Parses `request.SuperUsers` two ways — first as a Formio/CHEFS DataGrid JSON shape (`DataGridRowsValue`, matching a cell whose *key contains* "email", since the DataGrid column key varies per worksheet, e.g. `s03_SuperUserEmail`), falling back to a delimited string (`,`/`;`/`|`). Requires **at least one** email to resolve to a real user via `IOnboardingUserLookup`.|

Both the client (`ValidateAsync`, a pre-check the UI calls before enabling the approve button) and the server (`CreateTenantAsync`, unconditionally) run the same steps — the client result is explicitly not trusted, and is re-verified server-side even if the UI already showed green.

## `IOnboardingUserLookup` → `CssOnboardingUserLookup`

`FindUserGuidByEmailAsync(email) → string? IDIR GUID`, implemented by `src/Unity.GrantManager.Application/Identity/CssOnboardingUserLookup.cs` — a thin wrapper over `ICssUsersApiService.SearchUsersAsync("idir", email:)`, the host's existing CSS/IDIR directory integration. This is how a plain email address typed into an intake form becomes a real IDIR-backed program manager on the new tenant.

## `CreateTenantAsync` — the approval flow, step by step

`OnboardingRequestAppService.CreateTenantAsync(id, CreateTenantInputDto?)`:

1. Re-resolves field mappings and re-runs validation (defense in depth against a stale/tampered client state).
2. Parses `SuperUsers` → email list → resolves each via `IOnboardingUserLookup`; **throws** if zero resolve ("Cannot create tenant without at least one valid program manager").
3. Resolves feature checkboxes via `OnboardingFeatureMap.ResolveFeatureKeys` — a static dictionary mapping human-readable labels ("Payments", "AI Reporting") and camelCase checkbox-group keys (`aiReporting`) to real ABP feature keys (`Unity.Payments`, `Unity.AIReporting`, ...). Accepts either a JSON checkbox-group array (`[{"key":...,"value":true}]`) or a delimited string — mirroring the same dual-format tolerance as the super-users parsing.
4. Calls **`TenantAppService.CreateAsync(new TenantCreateDto{ Name, DisplayName, Branch, Division, Description, UserIdentifier = userGuids[0], FeatureKeys, MetabaseUserEmails = input?.MetabaseUserEmails })`** — the identical app service method the "New Tenant" modal uses, just populated from onboarding-request field data instead of a hand-filled form. There is no separate/elevated tenant-creation path for onboarding.
5. Any additional resolved super users beyond the first become tenant managers via a loop of `TenantAppService.AssignManagerAsync`.
6. Optionally folds `input.MetabaseNewDefaultUserEmails` / `MetabaseRemovedDefaultUserEmails` into the **global** `MetabaseSettings.UserEmails` list (add new, remove removed, de-duplicated case-insensitively) — the "save as default" checkbox on the approval modal's Metabase tab.
7. Calls `ApplicationProvider.CloseApplicationAsync(id)` → `OnboardingApplicationManager.TriggerAction(applicationId, GrantApplicationAction.Close)`, marking the source onboarding `Application` Closed so it drops out of the pending-onboarding queue.

Because step 4 is the exact same call the "New Tenant" modal makes, every downstream consequence documented for `TenantAppService.CreateAsync` — connection-string generation, Postgres role provisioning, the deferred post-tenant-creation job sequence — applies identically here. See [tenant-management-application-services.md](tenant-management-application-services.md#createasync--full-provisioning-sequence) and [tenant-management-post-creation.md](tenant-management-post-creation.md).

## Web UI

`Unity.TenantManagement.Web/Pages/TenantManagement/Onboarding/`, both page models `[Authorize(IdentityConsts.ITOperationsPolicyName)]`:

- **`Index.cshtml(.cs)`** — the onboarding request queue. `OnGet` is empty; all data is fetched client-side through `OnboardingRequestController`.
- **`CreateTenantModal.cshtml(.cs)`** — the approval modal. `OnGetAsync(id)` loads the request via `IOnboardingRequestAppService.GetAsync` (404s if not found) and preloads `DefaultMetabaseUserEmails` from the global setting to prefill the Metabase tab.
- **`OnboardingPageModel`** — shared abstract base (sets `ObjectMapperContext`).

Exposed over HTTP by a hand-written `OnboardingRequestController` (`api/onboarding-requests`) — see [tenant-management-application-services.md](tenant-management-application-services.md#ionboardingrequestappservice--onboardingrequestappservice) for the route table.

## Why IT Operations, specifically

The whole Onboarding surface — controller, both pages, the app service — is gated to the `ITOperations` role. This is the flip side of the "New Tenant" button being tightened to IT-Administrator-only (see [tenant-management-web-ui.md](tenant-management-web-ui.md)): IT Operations creates tenants exclusively through this reviewed, form-driven, validated path; IT Administrators can also use the direct modal. Both ultimately converge on the same `TenantAppService.CreateAsync` call, so neither path is a "lesser" way to create a tenant — Onboarding is a guided front-end onto the same provisioning logic, with the added structure of an approvable request queue, admin-configurable field mapping, and pre-flight validation.
