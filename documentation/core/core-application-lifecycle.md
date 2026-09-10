# Core — Application Lifecycle

The `Application` aggregate and the workflow it moves through. This is the spine of the product: Payments pays against its `ApprovedAmount`, AI drafts analysis onto it, Notifications emails on its transitions, Reporting reads it.

## The aggregate

`Domain/Applications/Application.cs` — `FullAuditedAggregateRoot<Guid>`, `IMultiTenant`, roughly 60 properties.

Four navigation properties use a **throw-on-uninitialized** pattern rather than nullable references, so a missed `Include` fails loudly instead of silently null-referencing:

```csharp
public virtual ApplicationForm ApplicationForm
{
    set => _applicationForm = value;
    get => _applicationForm
        ?? throw new InvalidOperationException("Uninitialized property: " + nameof(ApplicationForm));
}
```

`ApplicationForm`, `Applicant`, `ApplicationStatus` and `Assessment.Application` all do this. Collections (`Assessments`, `ApplicationTags`, `ApplicationAssignments`, `ApplicationLinks`) are plain nullable collections.

| Group | Properties |
|---|---|
| Identity | `ReferenceNo` (CHEFS confirmation id), `UnityApplicationId`, `ProjectName`, `ApplicationFormId`, `ApplicantId` |
| Status | `ApplicationStatusId` / `ApplicationStatus`, `SubStatus`, `ExternalStatusVisibility` |
| Money | `RequestedAmount`, `RecommendedAmount`, `ApprovedAmount`, `TotalProjectBudget`, `PercentageTotalProjectBudget`, `ProjectFundingTotal` |
| Dates | `SubmissionDate`, `ProposalDate`, `AssessmentStartDate`, `FinalDecisionDate`, `DueDate`, `NotificationDate`, `ProjectStartDate`, `ProjectEndDate`, `AssessmentResultDate`, `ContractExecutionDate` |
| Decision | `AssessmentResultStatus`, `DueDiligenceStatus`, `LikelihoodOfFunding`, `DeclineRational`, `RiskRanking`, `TotalScore`, `EligibleForRenewal` |
| Place | `City`, `Community`, `CommunityPopulation`, `EconomicRegion`, `RegionalDistrict`, `Place`, `ElectoralDistrict`, `ApplicantElectoralDistrict` |
| Signing authority | Five `SigningAuthority*` fields, `ContractNumber` |
| Sector | `Acquisition`, `Forestry`, `ForestryFocus` |
| Other | `Payload` (`jsonb`), `ProjectSummary`, `Notes`, `AIAnalysis`, `OwnerId`/`Owner`, `DefaultSiteId` |

`ElectoralDistrict` and `ApplicantElectoralDistrict` are different things and the entity says so in a comment: the first is the *project's* district, the second the *applicant's*, derived from whichever address type the form is configured to use.

`DefaultSiteId` points at a `Payments` site and is what payment creation requires — see [`payments/payments-suppliers-and-sites.md`](../payments/payments-suppliers-and-sites.md).

## The status is a table, not a column

`GrantApplicationState` is an enum in `Domain.Shared`, but the application stores `ApplicationStatusId` — a foreign key into the tenant's `ApplicationStatus` table. `ApplicationManager.TriggerAction` resolves the row after the workflow decides the new state:

```csharp
var statusChangedTo = await _applicationStatusRepository.GetAsync(x => x.StatusCode.Equals(statusChange));
application.ApplicationStatusId = statusChangedTo.Id;
application.ApplicationStatus   = statusChangedTo;
```

The enum carries a warning that the numbering is load-bearing:

```csharp
// WARNING: DO NOT EDIT ORDER NUMBER WITHOUT UPDATING DB CODE TABLE
```

Note also that `INITITAL_REVIEW_COMPLETED` is misspelled in the enum, and so is the corresponding code-table row.

## The workflow

Configured in `ApplicationManager.ConfigureWorkflow(StateMachine<GrantApplicationState, GrantApplicationAction>, bool isDirectApproval)`. Unlike the assessment workflow, it lives on the **domain service**, not the entity — it needs repositories and a permission checker.

### It is hierarchical

This is the structural fact that makes the table readable. Two superstates:

```text
OPEN  ──initial transition──▶ SUBMITTED
 │
 ├─ SUBMITTED · ASSIGNED · UNDER_INITIAL_REVIEW · INITITAL_REVIEW_COMPLETED
 ├─ UNDER_ASSESSMENT · ASSESSMENT_COMPLETED
 └─ DEFER · ON_HOLD

RESOLVED
 └─ CLOSED · WITHDRAWN · GRANT_APPROVED · GRANT_NOT_APPROVED
```

Every substate of `OPEN` inherits `OPEN`'s transitions — `Withdraw`, `Close`, `Internal_Unasign`, and (when the form allows it) direct `Approve`/`Deny`. That is why any open application can be withdrawn or closed without those transitions being repeated eleven times.

`RESOLVED` itself is configured with no transitions of its own; it exists as a grouping so `IsInFinalDecisionState()` and the substates can be reasoned about together.

### Transitions

| From | Action | To |
|---|---|---|
| `OPEN` (all substates) | `Withdraw` | `WITHDRAWN` |
| `OPEN` (all substates) | `Close` | `CLOSED` |
| `OPEN` (all substates) | `Internal_Unasign` | `SUBMITTED` |
| `SUBMITTED` | `Internal_Assign` | `ASSIGNED` |
| `SUBMITTED` | `Internal_StartAssessment` | `UNDER_ASSESSMENT` |
| `ASSIGNED` | `StartReview` | `UNDER_INITIAL_REVIEW` |
| `ASSIGNED` | `Internal_StartAssessment` | `UNDER_ASSESSMENT` |
| `UNDER_INITIAL_REVIEW` | `CompleteReview` | `INITITAL_REVIEW_COMPLETED` |
| `UNDER_INITIAL_REVIEW` | `Internal_StartAssessment` | `UNDER_ASSESSMENT` |
| `INITITAL_REVIEW_COMPLETED` | `StartAssessment` / `Internal_StartAssessment` | `UNDER_ASSESSMENT` |
| `UNDER_ASSESSMENT` | `CompleteAssessment` | `ASSESSMENT_COMPLETED` |
| `ASSESSMENT_COMPLETED` | `Approve` | `GRANT_APPROVED` |
| `ASSESSMENT_COMPLETED` | `Deny` | `GRANT_NOT_APPROVED` |
| `SUBMITTED` … `ASSESSMENT_COMPLETED` | `Defer` | `DEFER` |
| `SUBMITTED` … `ASSESSMENT_COMPLETED` | `OnHold` | `ON_HOLD` |
| `DEFER` / `ON_HOLD` | `StartReview`, `CompleteReview`, `StartAssessment`, `CompleteAssessment`, `Close`, and each other | back into the pipeline |
| `CLOSED` / `WITHDRAWN` | `Withdraw` / `Close`, `Defer`, `OnHold` | reopen |
| `GRANT_APPROVED` | `Withdraw`, `Close` | `WITHDRAWN` / `CLOSED` |
| `GRANT_APPROVED` | `Defer` — **requires `Approvals.DeferAfterApproval`** | `DEFER` |
| `GRANT_NOT_APPROVED` | `Close` | `CLOSED` |

`DEFER` and `ON_HOLD` are parking states, not terminal ones: both can return to any point in the review pipeline, and each can move to the other.

Only one transition in the whole machine is permission-guarded: deferring an already-approved grant.

### Direct approval

Some programs skip review and assessment entirely. `ApplicationForm.IsDirectApproval` is passed into `ConfigureWorkflow` and unlocks `Approve`/`Deny` from `OPEN` — and therefore from every open substate — plus from `CLOSED`, `WITHDRAWN`, and each other:

```csharp
.PermitIf(GrantApplicationAction.Approve, GrantApplicationState.GRANT_APPROVED,
          () => AllowDirectDecision(isDirectApproval, stateMachine, GrantApplicationState.GRANT_APPROVED),
          "Direct Approval Bypass");

private static bool AllowDirectDecision(bool isDirectApproval, StateMachine<…> stateMachine, GrantApplicationState targetState)
    => isDirectApproval && stateMachine.State != targetState;
```

The `stateMachine.State != targetState` term exists to prevent a reentrant transition — approving an already-approved application.

### Internal actions

Four actions are prefixed `Internal_` and are surfaced separately from user actions. `GetActions` marks them:

```csharp
IsInternal = trigger.ToString().StartsWith("Internal_")
```

They are fired by the system, not chosen by a user: `Internal_Assign` and `Internal_Unasign` from assignment changes, `Internal_StartAssessment` when the first assessment is created.

`GrantApplicationAppService.GetActions(applicationId, includeInternal = false)` hides them from the UI by default.

## Firing an action

`ApplicationManager.TriggerAction(applicationId, triggerAction)` validates twice before touching the state machine:

```csharp
if (triggerAction == Deny && application.DeclineRational.IsNullOrEmpty())
    throw new UserFriendlyException("The \"Decline Rationale\" is Required for application denial");

if ((triggerAction == Approve || triggerAction == Deny) && application.FinalDecisionDate == null)
    throw new UserFriendlyException("The Decision Date is Required.");
```

Then, as in the Payments module, the workflow runs against a **local copy** of the status purely as a gate, and the real write happens afterwards:

```csharp
var statusChange = application.ApplicationStatus.StatusCode;
var Workflow = new UnityWorkflow<…>(() => statusChange, s => statusChange = s,
                                    sm => ConfigureWorkflow(sm, application.ApplicationForm.IsDirectApproval));
await Workflow.ExecuteActionAsync(triggerAction);        // throws if not permitted
// resolve the ApplicationStatus row for statusChange and assign it
```

One side effect is applied inline: `StartAssessment` or `Internal_StartAssessment` stamps `AssessmentStartDate = DateTime.UtcNow`. `LastModificationTime` is also set by hand, with the comment *"This was not being updated"*.

`GetWorkflowDiagram(isDirectApproval)` renders the configured machine as a Mermaid graph — the fastest way to check the effect of a change to `ConfigureWorkflow`.

## Assignments drive two automatic transitions

Two business rules, called out in comments in four separate methods:

> **If an application is in the SUBMITTED state and has a user assigned, move to the ASSIGNED state.**
> **IF an application has all of its assignees removed, set the application status back to SUBMITTED.**

They are implemented in `AssignUserAsync`, `UpdateAssigneeAsync`, `RemoveAssigneeAsync` and `SetAssigneesAsync`, each wrapping its work in its own unit of work and firing `Internal_Assign` or `Internal_Unasign`.

`ApplicationAssignment` carries an optional `Duty`. `AssignUserAsync` resolves the assignee through `IPersonRepository` and throws `BusinessException("Tenant User Missing!")` if the person does not exist in the tenant.

`SetAssigneesAsync` is the bulk form used by the assignee-selection modal: it diffs the requested set against the current one, deletes what is gone, inserts what is new, and then fires a transition only if the *presence* of assignees changed.

An **owner** is separate from assignees — `Application.OwnerId` / `Owner`, managed by `InsertOwnerAsync` / `DeleteOwnerAsync`, and does not affect state.

## Field updates are tiered by permission

The entity does not expose public setters for the fields that matter. It exposes four grouped update methods, and the grouping *is* the authorization model — each corresponds to a different permission tier:

| Method | Fields | Gate |
|---|---|---|
| `UpdateAlwaysChangeableFields` | `Notes`, `SubStatus`, `LikelihoodOfFunding`, `TotalProjectBudget`, `NotificationDate`, `RiskRanking` | Editable at any time |
| `UpdateFieldsOnlyForPreFinalDecision` | `DueDiligenceStatus`, `RecommendedAmount`, `DeclineRational` | Only before a final decision |
| `UpdateApprovalFieldsRequiringPostEditPermission` | `ApprovedAmount` | Needs post-decision edit permission |
| `UpdateAssessmentResultFieldsRequiringPostEditPermission` | `RequestedAmount`, `TotalScore` | Needs post-decision edit permission |

`IsInFinalDecisionState()` tests membership of `GrantApplicationStateGroups.FinalDecisionStates` — `GRANT_APPROVED`, `GRANT_NOT_APPROVED`, `CLOSED`, `WITHDRAWN`, `RESOLVED`.

Two fields recalculate a derived value when they change: `TotalProjectBudget` and `RequestedAmount` both call `UpdatePercentageTotalProjectBudget()`.

`UpdateAssessmentResultStatus` stamps `AssessmentResultDate` only when the status actually changes.

### Validators on the entity

| Method | Rule |
|---|---|
| `ValidateAndSetDueDate` | Cannot be more than a day in the past |
| `ValidateAndSetFinalDecisionDate` | Cannot be in the future |
| `ValidateAndSetApprovedAmount` | Cannot be zero |
| `ValidateDirectApprovalRecommendedAmount` | Recommended amount cannot be zero — **unless** the form is direct-approval |

All throw `BusinessException`, and all compare against the current value first, so setting a field to the value it already holds never fails validation.

## Application links

`ApplicationLink` is a typed edge between two applications: `Parent`, `Child` or `Related` (`ApplicationLinkType`). `ApplicationLinksAppService` (605 lines) manages them and is what `IApplicationLinksService` exposes to modules.

The link type has real consequences outside the core:

- **Payments** treats a parent and its children as sharing one funding envelope — the remaining amount on any of them is the parent's approved amount less everything paid or pending across the whole group.
- **Form hierarchy** (`ApplicationForm.FormHierarchy`, `ParentFormId`) is validated against the actual links at payment time: a child-form application with no parent link, or one whose parent's form does not match `ParentFormId`, cannot be paid.

See [`payments/payments-approval-workflow.md`](../payments/payments-approval-workflow.md#amount-validation-before-creation).

## The application app service

`GrantApplicationAppService` is **1,388 lines** and is the widest surface in the core. Its responsibilities, in the order they appear:

| Area | Methods |
|---|---|
| Listing and reading | `GetListAsync` (250 lines), `GetAsync`, `GetBasicAsync`, `GetApplicationListAsync`, `GetApplicationDetailsListAsync`, `GetAllApplicationsAsync` |
| Assessment results | `UpdateAssessmentResultsAsync`, `UpdateExternalStatusVisibilityAsync` — `[Authorize(UnitySelector.Review.AssessmentResults.Update.Default)]` |
| Project info | `UpdateProjectInfoAsync`, `UpdatePartialProjectInfoAsync` — `[Authorize(UnitySelector.Project.UpdatePolicy)]` |
| Funding agreement | `UpdateFundingAgreementInfoAsync` |
| Payments seam | `UpdateSupplierNumberAsync` — `[Authorize(UnitySelector.Payment.Supplier.Update)]` |
| Assignment | `UpdateAssigneesAsync`, `InsertOwnerAsync`, `DeleteOwnerAsync` |
| Workflow | `GetActions`, `TriggerAction`, `UpdateApplicationStatus` (bulk) |
| AI seam | `DismissAIAnalysisItemAsync`, `RestoreAIAnalysisItemAsync` |
| Lookups | `GetApplicationStatusAsync`, `GetAccountCodingIdFromFormIdAsync`, `IsApplicantRedStopAsync`, `GetFormSubmissionByApplicationId` |

Authorization is applied per method with `UnitySelector` policies rather than a single class-level attribute, which is how field-level editing rights are enforced — the `UpdatePartialProjectInfoAsync` variant exists so the UI can save one field without needing the permissions for all of them.
