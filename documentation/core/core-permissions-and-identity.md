# Core — Permissions and Identity

Unity's authorization model is finer-grained than most ABP applications: permissions do not stop at "can open the applications page", they reach down to individual fields and widgets on the application detail screen. The mechanism is a single constants tree, `UnitySelector`, used simultaneously as permission names, zone names, and front-end element ids.

## `UnitySelector`

`modules/Unity.SharedKernel/Constants/UnitySelector.cs` — 271 lines, 111 constants, spread across three partial-class files. Its own summary states the intent:

> The purpose of this constants class is to set conventional semantics around **actions, permissions, zones, and front-end element IDs**.

```csharp
public static string[] GetAll() => ReflectionHelper.GetPublicConstantsRecursively(typeof(UnitySelector));
public static string ElementId(this string value) => value.Replace('.', '_');
```

`GetAll()` is how the permission definition provider enumerates the tree without listing it twice. `ElementId()` turns a permission name into a DOM id — `Unity.GrantManager.ApplicationManagement.Applicant.Summary` becomes `Unity_GrantManager_ApplicationManagement_Applicant_Summary` — so a page element, the zone that renders it, and the permission that gates it all share one identifier.

### Shape

Nested static partial classes, each level adding a segment, with a conventional `Default` / `Create` / `Update` / `Delete` quartet at the leaves:

```csharp
public static partial class Applicant
{
    public const string Default       = "Unity.GrantManager.ApplicationManagement.Applicant";
    public const string UpdatePolicy  = "…Applicant.UpdatePolicy";   // Custom Policy

    public static partial class Summary
    {
        public const string Default = "…Applicant.Summary";
        public const string Create  = "…Applicant.Summary.Create";
        public const string Update  = "…Applicant.Summary.Update";
        public const string Delete  = "…Applicant.Summary.Delete";
    }
    // Authority · Contact · AdditionalContact · Location
}
```

Top-level groups: `Applicant`, `Application`, `Review`, `Project`, `Funding`, `Payment`, `ApplicantManagement`, `SettingManagement`.

`UpdatePolicy` entries are **custom authorization policies** rather than plain permissions — `UnitySelector.Project.UpdatePolicy` and `UnitySelector.Applicant.UpdatePolicy` gate `GrantApplicationAppService.UpdateProjectInfoAsync` and the applicant equivalents, and are resolved through a policy provider rather than a straight permission check.

### The `Override` file

`UnitySelector.Override.cs` adds a further tier: permissions that let a user do something the normal rules forbid.

| Constant | Escalates |
|---|---|
| `Review.Approval.Update.UpdateFinalStateFields` | Editing approval fields **after** a final decision |
| `Review.AssessmentResults.Update.UpdateFinalStateFields` | Editing assessment-result fields after a final decision |
| `Review.AssessmentResults.Update.UpdateEligibleForRenewal` | Setting the renewal flag |
| `Review.AssessmentReviewList.Update.SendBack` / `.Complete` | The two assessment workflow actions |
| `Applicant.Summary.Update_AssignApplicant` | Reassigning an application's applicant |
| `Project.Summary.Update.*` | Field-level project overrides |

This is what backs the field-update tiering on the `Application` aggregate — see [core-application-lifecycle.md](core-application-lifecycle.md#field-updates-are-tiered-by-permission). The entity groups fields by *when* they may be edited; these permissions decide *who* may edit them outside that window.

`UnitySelector.ApplicantManagement.cs` holds the applicant-administration subtree (`Applicant.Default` / `.Update` / `.Delete`, `Addresses.Update`).

## The same string is a permission and a zone

The `Review.AssessmentReviewList.Update.SendBack` example shows the composition trick used by assessments:

```csharp
Name = $"{UnitySelector.Review.AssessmentReviewList.Update.Default}.{triggerAction}"
```

The requirement name is **built at runtime** from a selector prefix plus the workflow action. The same convention lets `DefaultZoneDefinition` name every zone with a selector constant — see [core-zones-and-ui-config.md](core-zones-and-ui-config.md).

## Roles

`UnityRoles` (`Domain.Shared/Identity/`) defines eleven:

```text
program_manager · reviewer · assessor · team_lead · approver · external_assessor
system_admin · financial_analyst · l1_approver · l2_approver · l3_approver
```

`DefinedRoles` is the canonical list. Two further role names live in `Unity.SharedKernel`'s `IdentityConsts` and are used as raw role checks rather than permissions:

| Role | Used by |
|---|---|
| `ITOperations` | Notification logs, Unity Messaging, the AI Prompts admin area |
| `ITAdmin` | AI Reporting override, metrics endpoint |

## Seeding permissions to roles

`PermissionGrantsDataSeeder` (393 lines) is the single source of truth for what each role can do out of the box. It calls the seeder once per role:

```csharp
await _permissionDataSeeder.SeedAsync(
    RolePermissionValueProvider.ProviderName, UnityRoles.ProgramManager, new[] { … });
```

Ten roles are seeded — every `UnityRoles` value except `FinancialAnalyst`, whose grants come from elsewhere. The three payment approver roles get their `PaymentsPermissions.Payments.L*ApproveOrDecline` grants here, which is how a module's permissions reach a core role.

`PermissionDataSeeder` itself is a `[Dependency(ReplaceServices = true)]` override of ABP's, and the reason is in the implementation: it wraps the whole seed in `CurrentTenant.Change(tenantId)` and inserts only permissions not already granted (`names.Except(existsPermissionGrants)`), making re-seeding idempotent and tenant-correct.

`IdentitySeedPermissions` and `SettingManagementSeedPermissions` hold the two grant sets that are not part of the `UnitySelector` tree.

## Identity

Authentication is **Keycloak** via OpenID Connect; Unity implements no password flow of its own. Identity data lives in the host database through `GrantManagerDbContext` (see [core-persistence.md](core-persistence.md#the-host-context-replaces-abps)).

| Component | Role |
|---|---|
| `Application/Identity/` | User lookup, delegation and the JWT app service |
| `JWTTokenAppService` | Issues tokens for service-to-service calls, including the applicant portal |
| `Integrations/Css/CssApiService` | The **CSS** (Common Hosted Single Sign-On) API — user search and role management in Keycloak, with a token cache that refreshes when the current token expires within five minutes |
| `Person` / `IPersonRepository` | The tenant-local mirror of a user, referenced by assignments, owners and assessments |
| `AbpClaimsPrincipalFactoryOptions` | Configured in `GrantManagerWebModule` to add Unity's own claims |

`AIScoringConstants.AiPersonId` — the fixed GUID `00000000-0000-0000-0000-000000000001` — is a `Person` row seeded per tenant so AI assessments have an assessor that satisfies the same foreign keys as a human one.

## Where authorization is applied

Five distinct mechanisms, and it is worth knowing which is which when tracing a "why can't I see this?" report:

| Mechanism | Example | Notes |
|---|---|---|
| **Method attribute** | `[Authorize(UnitySelector.Review.AssessmentResults.Update.Default)]` | The common case, applied per app-service method rather than per class |
| **Custom policy** | `[Authorize(UnitySelector.Project.UpdatePolicy)]` | Resolved by a policy provider, not a permission grant |
| **Resource-based** | `AuthorizationService.CheckAsync(assessment, requirement)` | `AssessmentAuthorizationHandler` — ownership matters, see [core-assessment.md](core-assessment.md#resource-based-authorization) |
| **Zone tag helper** | `<zone name="…">` | Combines permission, feature and zone configuration — see [core-zones-and-ui-config.md](core-zones-and-ui-config.md) |
| **Role check** | `.OnlyWhenInRole(IdentityConsts.ITOperationsRoleName)` | Menu items and a few controllers |

Feature flags are a **separate** axis from permissions. A feature says what the tenant has; a permission says what the user may do. Most module permissions are declared `.RequireFeatures(...)` so they disappear from the role editor when the feature is off — see [`ai/ai-overview.md`](../ai/ai-overview.md#the-gating-model) for the fullest worked example.

## Permission administration

`PermissionRoleMatrixRepository` (214 lines) backs the role/permission matrix screen — a grid of every permission against every role, which is how a system administrator sees the whole `UnitySelector` tree at once.
