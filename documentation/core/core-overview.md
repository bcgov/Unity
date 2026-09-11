# Core Overview

## What the core is

Strip away every module and what remains is a system for **receiving grant applications and deciding them**. That is the core: intake from CHEFS, an application record, a review and assessment workflow, a decision, and the applicant and form records those hang off.

Everything else in the product is a module attached to that spine:

```text
                        ┌──────────────────────────────┐
   CHEFS ──webhook──▶   │   INTAKE                     │
                        │   submission → Application   │
                        └───────────────┬──────────────┘
                                        ▼
     Unity.Flex ───custom fields───▶ ┌─────────────────────┐
     Unity.AI ─────analysis/scoring▶ │  APPLICATION        │ ◀── assignments, owners,
     Unity.Notifications ──email───▶ │  15-state workflow  │     links, tags, comments
                                     └──────────┬──────────┘
                                                ▼
     Unity.Flex ───scoresheets─────▶ ┌─────────────────────┐
                                     │  ASSESSMENT         │
                                     │  3-state workflow   │
                                     └──────────┬──────────┘
                                                ▼
                                        GRANT_APPROVED
                                                │
     Unity.Payments ◀──approved amount──────────┘
     Unity.Reporting ◀─reads everything
```

## Project layering

Standard ABP layering, with one thing worth noting up front: **`Unity.GrantManager.Domain.Shared` carries more than constants.** It holds the state and action enums for both workflows, the `UnityWorkflow` wrapper over Stateless, error codes, and the feature/setting keys — which is why modules can reference application states without depending on the Domain project.

```text
Domain.Shared          → GrantApplicationState/Action, AssessmentState/Action,
                         UnityWorkflow + extensions, GrantManagerDomainErrorCodes,
                         permission constants, localization
        ↑
Domain                 → Application, Applicant, Assessment, ApplicationForm aggregates;
                         ApplicationManager, AssessmentManager, ApplicantMergeManager,
                         ZoneManager; repository interfaces; permission seeders
        ↑
Application.Contracts  → DTOs, I*AppService interfaces, permission definition providers
        ↑
Application            → app services, intake pipeline, integrations, local event handlers,
                         background workers, attachments, dashboard
        ↑
EntityFrameworkCore    → GrantManagerDbContext + GrantTenantDbContext, repositories,
                         host and tenant migrations
        ↑
HttpApi                → CHEFS, applicant-portal and attachment controllers
        ↑
Web                    → Razor Pages, 36 shared view components, menus, middleware, theme
```

## Two databases

Unity is multi-tenant with **a database per tenant**, plus one host database.

| Context | Holds |
|---|---|
| `GrantManagerDbContext` (**host**) | Tenants, users and roles, permissions, dynamic URLs, CAS client codes, and the AI configuration tables (`AI.AIModels`, `AIOperations`, `AIPrompts`, `AIRequests`) |
| `GrantTenantDbContext` (**tenant**) | Applications, applicants, forms, assessments, attachments, comments, tags, history — plus every module's tenant tables (`Payments.*`, `Notifications.*`, Flex, `AI.GenerationReviews`) |

Migrations are split to match, and choosing the wrong one is the most common mistake when adding a table:

```bash
cd applications/Unity.GrantManager/src/Unity.GrantManager.EntityFrameworkCore

dotnet ef migrations add <Name> --context GrantManagerDbContext --output-dir Migrations/HostMigrations
dotnet ef migrations add <Name> --context GrantTenantDbContext  --output-dir Migrations/TenantMigrations
```

Nearly every core entity implements `IMultiTenant`, and ABP's automatic filter does the isolation — no core query filters on `TenantId` by hand. The places that deliberately step outside it use `ICurrentTenant.Change(...)`, and they are almost all background workers iterating every tenant.

## Modules compose into the host

The core does not reference modules through interfaces it defines; it takes a direct dependency on each one and wires it in at up to four points — application module, web module, DbContext, and (for some) the migrator.

```csharp
// GrantManagerApplicationModule
[DependsOn(
    typeof(FlexApplicationModule),
    typeof(NotificationsApplicationModule),
    typeof(PaymentsApplicationModule),
    typeof(AIApplicationModule),
    typeof(ReportingApplicationModule),
    …)]
```

`GrantTenantDbContext.OnModelCreating` then calls each module's model extension — `ConfigureFlex()`, `ConfigureNotifications()`, `ConfigurePayments()` — while `GrantManagerDbContext` calls `ConfigureAI()`. `GrantManagerWebModule` (738 lines) composes the web layer the same way.

The coupling runs both ways: modules reference core types (`IApplicationRepository`, `IApplicationFormRepository`, `IGrantApplicationAppService`, `IEndpointManagementAppService`) as freely as the core references theirs. Treat the core and its modules as one deployable, not as a host with plug-ins.

## Two state machines, one wrapper

Both workflows are built on the **Stateless** library behind a shared wrapper, `UnityWorkflow<TStates, TTriggers>` (`Domain.Shared/Workflow/`):

```csharp
public virtual async Task ExecuteActionAsync(TTriggers action)
{
    if ((await _stateMachine.GetPermittedTriggersAsync()).Contains(action))
        await _stateMachine.FireAsync(action);
    else
        throw new BusinessException("InvalidStateTransition",
            $"Cannot transition from {_stateMachine.State} via {action}");
}
```

`UnityWorkflowExtensions` adds `GetPermittedActionsAsync()`, `GetAllActions()` and `GetWorkflowDiagram()` (a Mermaid graph).

The two machines are configured in different places, and the difference matters:

| | Application | Assessment |
|---|---|---|
| States | 15, **hierarchical** (`OPEN`, `RESOLVED` superstates) | 3, flat |
| Configured in | `ApplicationManager.ConfigureWorkflow` — a domain **service** | `Assessment.ConfigureWorkflow` — on the **entity**, via `IHasWorkflow<,>` |
| Guarded by | Per-form `IsDirectApproval` flag and one permission check | Nothing |
| Entry/exit hooks | None | `OnEntry`/`OnExit` on `COMPLETED` |

`Unity.Payments` has its own near-identical copy of this wrapper (`PaymentsWorkflow`) for its approval ladder — see [`payments/payments-approval-workflow.md`](../payments/payments-approval-workflow.md).

## The request path

A typical staff request through the core:

```text
Browser
   ↓  Razor Page or ABP dynamic API proxy
Web            page model / view component / controller
   ↓
Application    *AppService — [Authorize(UnitySelector...)], DTO in, DTO out
   ↓
Domain         *Manager — business rules, workflow transitions
   ↓
EF Core        repository → GrantTenantDbContext (tenant-filtered)
```

External traffic takes a shorter path: the CHEFS webhook and the applicant-portal endpoints land directly on `HttpApi` controllers, several of which are `[AllowAnonymous]` and authenticate by other means. See [core-intake.md](core-intake.md).

## Core concepts (glossary)

| Term | Meaning |
|---|---|
| **`Application`** | The aggregate root of the product — one grant application. `FullAuditedAggregateRoot<Guid>`, ~60 properties, plus a `jsonb` `Payload` holding the raw submission shape. |
| **`GrantApplicationState`** | The 15-value application status enum, persisted through an `ApplicationStatus` **lookup table** rather than as a column. Ordering is load-bearing — the enum carries a warning not to renumber it. |
| **`ApplicationStatus`** | A tenant table mapping status codes to ids. `ApplicationManager.TriggerAction` resolves the row and assigns `ApplicationStatusId`. |
| **`Applicant`** | The organisation or individual applying. Shared across applications; deliberately **not** `ISoftDelete` (see [core-applicants.md](core-applicants.md#why-applicant-is-not-isoftdelete)). |
| **`ApplicantAgent`** | The person who actually submitted, with their BCeID identity, contact details and ordering. One per application. |
| **`ApplicationForm`** | A configured intake form, bound to a CHEFS form GUID. Carries the flags that change product behaviour: `IsDirectApproval`, `Payable`, `PreventPayment`, `ScoresheetId`, the AI toggles, the form hierarchy. |
| **`ApplicationFormVersion`** | One published version of that form: the CHEFS version GUID, the `SubmissionHeaderMapping` that maps CHEFS fields onto Unity fields, and the raw `FormSchema`. |
| **`ApplicationFormSubmission`** | The stored CHEFS submission for one application — the raw JSON, the CHEFS submission GUID, and the OIDC subject of the submitter. |
| **`Assessment`** | One assessor's review of one application. Many per application; at most one per assessor; at most one AI assessment. |
| **`ApplicationAssignment`** | A user assigned to an application, with an optional `Duty`. Assignment count drives two automatic state transitions. |
| **`ApplicationLink`** | A typed link between two applications — `Parent`, `Child` or `Related`. Parent/child links share a funding envelope in Payments. |
| **Zone** | A configurable UI region — tab, group or widget — on the application detail page. Definitions live in `Domain/Zones/`. |
| **`UnitySelector`** | The permission tree used across the product for granular, field-level authorization — 111 constants in a partial-class hierarchy, with an `Override` variant for permissions that can be escalated. |
| **`IntakeMapping`** | The intermediate shape a CHEFS submission is mapped into before an `Application` is built from it. |
| **Unity Application ID** | An optional human-readable identifier generated at intake from the form's `Prefix` plus either a tenant sequence or the CHEFS confirmation id. Distinct from `ReferenceNo`, which is always the CHEFS confirmation id. |

## Read in this order

1. **[core-overview.md](core-overview.md)** (this file).
2. **[core-intake.md](core-intake.md)** — how an application comes into being.
3. **[core-application-lifecycle.md](core-application-lifecycle.md)** — how it moves.
4. **[core-assessment.md](core-assessment.md)** — how it is judged.
5. **[core-applicants.md](core-applicants.md)** — who is applying.
6. **[core-forms-and-versions.md](core-forms-and-versions.md)** — what they applied on.
