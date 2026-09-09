# Payments Approval Workflow

This is the central flow of the module: from "an application is approved" to "the payment has left Unity". Everything here is driven by `PaymentRequest.Status` and the `ExpenseApproval` rows hanging off the payment.

## Creating a payment request

`PaymentRequestAppService.CreateAsync(List<CreatePaymentRequestDto>)`, `[Authorize(PaymentsPermissions.Payments.RequestPayment)]`.

For the whole list, one **batch setup** is resolved first (`GetBatchSetupAsync` → `PaymentRequestConfigurationManager`):

| Value | How it is derived |
|---|---|
| `paymentIdPrefix` | `PaymentConfiguration.PaymentIdPrefix` (empty string if no configuration row) |
| `batchNumber` | `max(BatchNumber) + 1` over **all** payment requests in the tenant, or `1` if none |
| `batchName` | `{prefix}_UNITY_BATCH_{batchNumber}` |
| `nextSequenceNumber` | last sequence segment of the most recent payment created this calendar year, `+ 1`; `1` if none |

Then, per item and index `i`:

```text
referenceNumberPrefix = {prefix}-{currentYear}
sequenceNumber        = (nextSequenceNumber + i).ToString("D4")
ReferenceNumber       = {referenceNumberPrefix}-{sequenceNumber}
InvoiceNumber         = {referenceNumberPrefix}-{originalInvoiceNumber}-{sequenceNumber}
```

`originalInvoiceNumber` is the application's reference number, set by the create-payments page. `ReferenceNumber` is uniquely indexed and is what the UI calls the **Payment ID**.

The `PaymentRequest` constructor generates Level 1 and Level 2 `ExpenseApproval` rows and runs `ValidatePaymentRequest()` (amount > 0, supplier number, site, account coding). Each created payment publishes a `PaymentStatusChangedEvent` on the local event bus, which the host's `ScheduledNotificationEventHandler` can pick up to send configured notifications.

Failures inside the loop are logged and **rethrown**, so a bad item aborts the batch — but items already inserted before it are not rolled back unless an ambient unit of work covers them.

### Historical payments

`CreateHistoricalAsync`, `[Authorize(PaymentsPermissions.Payments.AddHistoricalPayment)]`, follows the same batch/number generation but constructs the entity from `CreateHistoricalPaymentRequestDto`: status `HistoricalPayment`, `InvoiceStatus` and `PaymentStatus` set to `"Paid"`, `PaymentDate` from the user-supplied paid date, and **no expense approvals**. These payments never enter the approval workflow and are never sent to CAS; they exist so the payment history and the rollups reflect money paid before Unity (or outside it). The only transition available to them is `Cancel`.

## Amount validation before creation

Before a payment can be created, the create-payments page (`CreatePaymentRequestsModel`) and its helper (`PaymentRequestPageHelperService`) enforce the amount rules. This logic lives in the **web layer**, not in the domain.

`GetRemainingAmountAsync(application)`:

```text
remaining = ApprovedAmount
          - totalPaymentRequestAmount(application)
          - Σ totalPaymentRequestAmount(each child application)
```

where `totalPaymentRequestAmount` excludes declined, cancelled, and CAS-errored payments (see [payments-domain-model.md](payments-domain-model.md#repositories)). Child applications come from `IApplicationLinksService`.

`GetErrorListAsync(...)` blocks a payment when any of the following hold, disabling the row's fields in the modal:

- supplier, site, or supplier number missing (skipped for historical payments)
- the site's pay group is `EFT` but the site has no bank account
- remaining amount ≤ 0
- the application is not `GRANT_APPROVED`
- a linked **parent** application exists and is not `GRANT_APPROVED`
- the application's form is not `Payable`
- no account coding could be resolved (skipped for historical payments)
- the form is a `Child` in a form hierarchy but the application has no parent link, or the linked parent's form does not match `ApplicationForm.ParentFormId`

For parent/child groups the modal computes a shared ceiling — `parent.ApprovedAmount − (parent paid/pending + all children paid/pending)` — applies it to every row in the group (`PopulateParentChildValidationDataAsync`), and re-validates it server-side on POST (`ValidateParentChildPaymentAmountsAsync`) so a stale form cannot over-commit the envelope. Standalone rows are re-validated individually against the current remaining amount (`ValidateStandalonePaymentAmountsAsync`).

## The state machine

`PaymentsManager.ConfigureWorkflow(StateMachine<PaymentRequestStatus, PaymentApprovalAction>)` declares every legal transition, each guarded by a permission check:

| From | Trigger | To | Requires |
|---|---|---|---|
| `L1Pending` | `L1Approve` | `L2Pending` | `L1ApproveOrDecline` |
| `L1Pending` | `L1Decline` | `L1Declined` | `L1ApproveOrDecline` |
| `L1Pending` | `Cancel` | `Cancelled` | `CancelPayment` |
| `L1Declined` | `L1Approve` | `L2Pending` | `L1ApproveOrDecline` |
| `L2Pending` | `L2Approve` | `L3Pending` | `L2ApproveOrDecline` |
| `L2Pending` | `Submit` | `Submitted` | `L2ApproveOrDecline` |
| `L2Pending` | `L2Decline` | `L2Declined` | `L2ApproveOrDecline` |
| `L2Pending` | `Cancel` | `Cancelled` | `CancelPayment` |
| `L2Declined` | `L2Approve` | `L3Pending` | `L2ApproveOrDecline` |
| `L2Declined` | `Submit` | `Submitted` | `L2ApproveOrDecline` |
| `L3Pending` | `Submit` | `Submitted` | `L3ApproveOrDecline` |
| `L3Pending` | `L3Decline` | `L3Declined` | `L3ApproveOrDecline` |
| `L3Pending` | `Cancel` | `Cancelled` | `CancelPayment` |
| `HistoricalPayment` | `Cancel` | `Cancelled` | `CancelPayment` |

A declined payment is not terminal: re-approving at the same level puts it back on the path. There is no transition **out of** `L3Declined`.

`PaymentsWorkflow.ExecuteActionAsync` asks the machine for its currently permitted triggers and throws `BusinessException("InvalidStateTransition", "Cannot transition from {state} via {action}")` if the requested one is not among them.

### The state machine gates; the ladder decides

`PaymentsManager.TriggerAction` runs the workflow against a **local copy** of the status (`statusChange`), purely to validate the transition. It then computes the real target status with an `if/else` ladder and applies it to the entity:

```csharp
var Workflow = new PaymentsWorkflow<…>(() => statusChange, s => statusChange = s, ConfigureWorkflow);
await Workflow.ExecuteActionAsync(triggerAction);   // throws if not permitted
// … if/else ladder sets statusChangedTo and stamps the ExpenseApproval …
paymentRequest.SetPaymentRequestStatus(statusChangedTo);
```

The ladder also records the decision on the matching `ExpenseApproval`:

| Trigger | ExpenseApproval stamped | Resulting status |
|---|---|---|
| `L1Approve` | Level 1 → `Approve(currentUser)` | `L2Pending` |
| `L1Decline` | Level 1 → `Decline(currentUser)` | `L1Declined` |
| `L2Approve` | Level 2 → `Approve(currentUser)` | `L3Pending` |
| `L2Decline` | Level 2 → `Decline(currentUser)` | `L2Declined` |
| `L3Decline` | Level 3 → `Decline(currentUser)` | `L3Declined` |
| `Submit` | Level 2 if the caller holds L2 and status was `L2Pending`; Level 3 if the caller holds L3 and status was `L3Pending` | `FSB` or `Submitted` (see below) |

`PaymentApprovalAction.L3Approve` is declared in the enum but is never configured and never fired — the L3 approval is expressed as `Submit`.

`UpdatePaymentStatusAsync` wraps `TriggerAction` in its own unit of work and saves.

### The Submit fork: CAS or FSB

```csharp
bool preventPayment = await GetFormPreventPaymentStatusByApplicationId(paymentRequest.CorrelationId);

if (preventPayment)
{
    statusChangedTo = PaymentRequestStatus.FSB;
    paymentRequest.SetInvoiceStatus(CasPaymentRequestStatus.SentToAccountsPayable);
}
else
{
    statusChangedTo = PaymentRequestStatus.Submitted;
    await casPaymentRequestCoordinator.AddPaymentRequestsToInvoiceQueue(paymentRequest);
}
```

`PreventPayment` is a flag on the application's `ApplicationForm`, configured through the host's per-form payment configuration widget. `FSB` payments never reach CAS; they are picked up by the FSB notifier and emailed to the Financial Services Branch as a spreadsheet — see [payments-notifications.md](payments-notifications.md).

## How a level gets chosen: `DetermineTriggerActionAsync`

The UI does not send a trigger. It sends `{ PaymentRequestId, IsApprove, Note }` per payment and `PaymentRequestAppService.UpdateStatusAsync` works out which trigger applies, based on the payment's current status and the **caller's** permissions:

```text
if caller has L1 permission and status ∈ {L1Pending, L1Declined}
        → IsApprove ? L1Approve : L1Decline

else if caller has L2 permission and status ∈ {L2Pending, L2Declined}
        → !IsApprove                        → L2Decline
        → amount > threshold                → L2Approve   (routes to L3Pending)
        → otherwise                         → Submit      (skips L3)

else if caller has L3 permission and status ∈ {L3Pending, L3Declined}
        → IsApprove ? Submit : L3Decline

else    → PaymentApprovalAction.None (payment is skipped)
```

This is why a user holding several approval permissions always acts at the **lowest** level the payment is currently sitting at.

### Threshold resolution

`GetLevel2ApprovalActionAsync` resolves the threshold through `PaymentRequestConfigurationManager.GetPaymentRequestThresholdByApplicationIdAsync(applicationId, userPaymentThreshold)`:

```text
formThreshold = application.ApplicationForm.PaymentApprovalThreshold
userThreshold = PaymentThreshold row for the current user (null if none)

both present   → min(formThreshold, userThreshold)
one present    → that one
neither        → 0m
```

A threshold of `0` means every payment exceeds it, so every payment needs L3. Failures resolving the threshold are logged as a warning and leave `threshold` null, in which case the action falls through to `Submit` — the payment skips L3.

### Separation of duties (AB#26693)

The same person may not approve at L1 and then at L2. This is enforced twice in `UpdateStatusAsync`:

1. **Up front, for the whole batch** — if any payment being approved is in `L2Pending` and its Level 1 `DecisionUserId` equals the current user, the whole call throws `BusinessException(ErrorConsts.L2ApproverRestriction)` before anything is changed.
2. **Per payment**, inside `CanPerformLevel2ActionAsync`, the same check throws again.

The approval modal also surfaces this client-side: `PaymentsApprovalModel` carries `PreviousL1Approver` so rows the user cannot approve can be flagged before submitting.

### Batch behaviour and failure handling

`UpdateStatusAsync` iterates the requested payments and, per payment:

- appends the note (`existing; new` when both are non-empty, otherwise replaces),
- determines and fires the trigger,
- re-reads the payment, records it in `fsbPaymentIds` if it just entered `FSB`,
- publishes a `PaymentStatusChangedEvent` if the status actually changed,
- adds the refreshed DTO to the result.

Exceptions inside the loop are **logged and swallowed** — one payment failing does not abort the others, but the caller only learns about it by noticing the payment is missing from the returned list.

After the loop, if any payments entered `FSB`, `FsbPaymentNotifier.NotifyFsbPayments` is invoked; failures there are logged and deliberately not rethrown, because an email failure must not undo an approval.

## Level 3 slots are created on demand

Level 3 `ExpenseApproval` rows are **not** created with the payment. They are added or removed by the approval modal's page model, `UpdatePaymentRequestStatus.UpdateExpenseApprovalsAsync`, while the modal is being rendered:

- the payment is in `L2Pending`, and
- `isL3ApprovalRequired = payment.Amount > PaymentThreshold` — where `PaymentThreshold` is the same `min(form, user)` resolution computed per row in `BuildPaymentApprovalsAsync` —
- then a Level 3 row is inserted if required and absent, or deleted if present and not required.

The modal then computes the destination status for display (`GetNextStatus`) and filters out any row the current user lacks the permission for (`VerifyPermissionsAsync` / `GetRequiredPermission`). Note that this write happens during a `GET`.

## Cancellation

`PaymentRequestAppService.CancelAsync(Guid)`, `[Authorize(PaymentsPermissions.Payments.CancelPayment)]`:

1. Load the payment; `BusinessException("Payments:PaymentRequestNotFound")` if missing.
2. Check status against the eligible set — `HistoricalPayment`, `L1Pending`, `L2Pending`, `L3Pending` — otherwise `BusinessException("Payments:CancellationNotAllowed")` with the current status. A payment already `Submitted` to CAS cannot be cancelled in Unity.
3. `PaymentsManager.CancelPaymentAsync` (its own `[UnitOfWork]`) runs the `Cancel` trigger through the state machine, sets `Status = Cancelled`, and stamps `SetCancellation(Clock.Now, currentUser.Id, "{Name} {SurName}")`.
4. For a **historical** payment it additionally sets `InvoiceStatus = Cancelled` and `PaymentStatus = "Not Paid"` so the rollup stops counting it as paid.
5. A `PaymentStatusChangedEvent` is published.

## Reading the workflow from the UI

`PaymentsManager.GetActions(paymentRequestId)` returns every configured trigger paired with whether it is currently permitted for this payment and this user:

```csharp
new PaymentActionResultItem {
    PaymentApprovalAction = trigger,
    IsPermitted           = permittedActions.Contains(trigger),
    IsInternal            = trigger.ToString().StartsWith("Internal_")
}
```

`UnityWorkflowExtensions.GetWorkflowDiagram()` can render the configured machine as a DOT graph, which is useful when reasoning about changes to `ConfigureWorkflow`.
