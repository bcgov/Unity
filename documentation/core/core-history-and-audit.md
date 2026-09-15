# Core — History and Audit

"History" means three different things in Unity, backed by three different mechanisms. Telling them apart is most of what this document is for.

| Kind | Source | Answers |
|---|---|---|
| **Change history** | ABP's audit log | Who changed which field, when |
| **Funding history** | `FundingHistory` entity | What this applicant has been granted before |
| **Audit history** | `AuditHistory` entity | External audits performed on this applicant |

The first is automatic and system-generated. The other two are **manually maintained records** that happen to share the word.

## Change history

Unity does not write its own change log. It reads ABP's audit log, which records entity changes automatically because auditing is enabled in `GrantManagerWebModule` (`AbpAuditingOptions`, `AbpAspNetCoreAuditingOptions`, `UseAuditing()`).

### Reading it

`HistoryAppService` queries `IAuditLogRepository.GetEntityChangeListAsync(...)` and projects the result for the History widget. Two implementation details matter:

- **Its own methods are marked `[DisableEntityChangeTracking]`.** Reading history must not itself generate history.
- **User names are resolved and cached per call.** `LookupUserNameAsync(auditLogId)` is memoised in a local dictionary keyed by audit log id, because a page of changes typically comes from a handful of users and the lookup is a separate query.

`ExtendedEfCoreAuditLogRepository` (`[ExposeServices(typeof(IExtendedAuditLogRepository))]`) extends ABP's repository with `GetEntityChangeByTypeWithUsernameAsync(entityId, entityTypeFullNames, ct)` — filtering by **entity type full name** and joining the username in one pass, returning `EntityChangeWithUsername`.

### Filtering by type is how payment history works

`HistoryConsts` pins the two Payments entity types by full name:

```csharp
public const string ExpenseApprovalObject = "Unity.Payments.Domain.PaymentRequests.ExpenseApproval";
public const string PaymentRequestObject  = "Unity.Payments.Domain.PaymentRequests.PaymentRequest";
public static List<string> PaymentEntityTypeFullNames => [ExpenseApprovalObject, PaymentRequestObject];
```

`PaymentHistoryAppService` uses that list to pull just the payment-related changes, which is what backs the **Payment History** page (`Web/Pages/PaymentHistory/Details.cshtml`) reached from the Payments list's History button.

Note the coupling: these are **hard-coded fully-qualified type names in the core**, so renaming or moving either Payments class silently empties the payment history view. Nothing at compile time connects them.

### Why background consumers care about audit scope

Because the audit log is built from entity changes collected during `SaveChangesAsync`, code running outside a request has to complete its unit of work inside an audit scope or the changes go unrecorded. The Payments CAS coordinator documents exactly this, and it is why several background paths open a unit of work explicitly and complete it themselves rather than relying on an ambient one — see [`payments/payments-cas-integration.md`](../payments/payments-cas-integration.md).

## Funding history

`FundingHistory` is a manually maintained record of what an applicant has previously been granted — it is not derived from applications or payments.

| Property | |
|---|---|
| `ApplicantId` | Owner |
| `GrantCategory`, `FundingYear` | Which program and year |
| `RenewedFunding` | Whether it was a renewal |
| `ApprovedAmount`, `OneTimeConsideration`, `ReconsiderationAmount`, `TotalGrantAmount` | The money |
| `PaidDate` | |
| `FundingNotes` | Free text |

The four amount fields are separate because they mean different things to program staff, and `TotalGrantAmount` is stored rather than computed.

`Applicant.FundingHistoryComments` holds a free-text note about the applicant's funding history as a whole, separate from the per-row `FundingNotes`.

## Audit history

`AuditHistory` records external audits of an applicant.

| Property | |
|---|---|
| `ApplicantId` | Owner |
| `AuditTrackingNumber`, `AuditDate`, `AuditorName` | Identity of the audit |
| `AuditStatus` | `AuditHistoryStatus` — `InProgress` or `Completed` |
| `AuditNote` | Free text |

Again there is a companion field on the applicant, `Applicant.AuditComments`.

Two sibling records follow the same pattern: `IssueTracking` (with `Applicant.IssueTrackingComments`) and `ReportsHistory` (with `Applicant.ReportsComments`). All four are applicant-scoped manual registers with a matching comments field on the applicant record.

## Where history surfaces

| Surface | Shows |
|---|---|
| `HistoryWidget` | Change history for the current application, from the audit log |
| `ApplicantHistory` widget and `Web/Pages/ApplicantHistory/` | The applicant's funding, audit, issue-tracking and reports registers |
| `Web/Pages/PaymentHistory/Details.cshtml` | Change history filtered to the two Payments entity types |
| `EmailHistoryWidget` | Sent-email history, from the Notifications module's `EmailLog` |

The last one is worth noting as a fourth kind of history again — it reads `EmailLog` rows rather than any of the mechanisms above. See [`notifications/notifications-web-ui.md`](../notifications/notifications-web-ui.md).
