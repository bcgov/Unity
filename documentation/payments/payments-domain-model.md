# Payments Domain Model

Everything in this document lives under `applications/Unity.GrantManager/modules/Unity.Payments/src/`. The domain model is in `Unity.Payments.Application/Domain/` — this module has no separate `.Domain` project (see [payments-overview.md](payments-overview.md#module-layout-and-dependency-direction)).

## Entities

### `PaymentRequest` — `Domain/PaymentRequests/PaymentRequest.cs`

`FullAuditedAggregateRoot<Guid>`, `IMultiTenant`, `ICorrelationEntity`. The aggregate root of the module.

| Group | Properties |
|---|---|
| Identity & amount | `InvoiceNumber`, `ReferenceNumber` (unique), `Amount`, `Description`, `Note` |
| State | `Status` (`PaymentRequestStatus`, defaults to `L1Pending`), `IsRecon` |
| Correlation | `CorrelationId` (the application id), `CorrelationProvider` (`"Application"`), `SubmissionConfirmationCode` (the application's reference no) |
| Payee snapshot | `PayeeName`, `ContractNumber`, `SupplierName`, `SupplierNumber`, `RequesterName` |
| Batch | `BatchName`, `BatchNumber` |
| Relationships | `SiteId` / `Site`, `AccountCodingId` / `AccountCoding`, `ExpenseApprovals`, `PaymentTags` |
| CAS results | `InvoiceStatus`, `PaymentStatus`, `PaymentNumber`, `PaymentDate`, `CasHttpStatusCode`, `CasResponse` |
| FSB tracking | `FsbNotificationEmailLogId`, `FsbNotificationSentDate`, `FsbApNotified` (`"Yes"` or null) |
| Cancellation | `CancelledOn`, `CancelledById`, `CancelledBy` |

`IsApproved` is a computed property — `ExpenseApprovals.All(s => s.Status == Approved)`. Note that this is `true` for a historical payment, which has **no** approvals at all.

Two public constructors:

- `PaymentRequest(Guid, CreatePaymentRequestDto)` — normal path. Generates Level 1 and Level 2 `ExpenseApproval` rows, then calls `ValidatePaymentRequest()`.
- `PaymentRequest(Guid, CreateHistoricalPaymentRequestDto)` — sets `Status = HistoricalPayment`, `InvoiceStatus = PaymentStatus = "Paid"`, `PaymentDate` from the supplied paid date, and creates **no** expense approvals. Only checks `Amount > 0`.

`ValidatePaymentRequest()` throws `BusinessException` for: amount ≤ 0 (`ZeroPayment`), blank supplier number (`MissingSupplierNumber`), missing site (`MissingSite`), missing account coding (`MissingAccountCoding`).

State is changed only through fluent `Set*` methods (`SetPaymentRequestStatus`, `SetInvoiceStatus`, `SetPaymentStatus`, `SetPaymentNumber`, `SetPaymentDate`, `SetCasHttpStatusCode`, `SetCasResponse`, `SetAmount`, `SetNote`, `SetComment`, `SetFsbNotificationEmailLog`, `ClearFsbNotificationEmailLog`, `SetCancellation`). `SetPaymentDate` parses CAS's `dd-MMM-yyyy` format and normalises it to `yyyy-MM-dd`, falling back to the raw string if parsing fails.

### `ExpenseApproval` — `Domain/PaymentRequests/ExpenseApproval.cs`

`FullAuditedEntity<Guid>`, `IMultiTenant`. One approval slot.

- `Type` — `ExpenseApprovalType` (`Level1`, `Level2`, `Level3`)
- `Status` — `ExpenseApprovalStatus` (`Requested`, `Approved`, `Declined`), defaults to `Requested`
- `DecisionUserId`, `DecisionDate` — **write-once**: both setters throw `InvalidOperationException` if the field is already non-null. A slot that has been declined therefore cannot later record a different approver.
- `Approve(Guid)` / `Decline(Guid)` set status, user and `DateTime.UtcNow` together.

### `Supplier` — `Domain/Suppliers/Supplier.cs`

`FullAuditedAggregateRoot<Guid>`, `IMultiTenant`. A mirror of a CAS supplier: `Name`, `Number`, `Subcategory`, `SIN`, `ProviderId`, `BusinessNumber`, `Status`, `SupplierProtected`, `StandardIndustryClassification`, `LastUpdatedInCAS`, a mailing address (`MailingAddress`, `City`, `Province`, `PostalCode`), and a `Sites` collection.

Updates go through value-object methods — `UpdateBasicInfo(SupplierBasicInfo)`, `UpdateProviderInfo(ProviderInfo)`, `UpdateStatus(SupplierStatus)`, `UpdateCasMetadata(CasMetadata)`, `SetAddress(...)` — with the value objects defined in `Domain/Suppliers/ValueObjects/` (`Address`, `MailingAddress`, `SupplierBasicInfo`, `ProviderInfo`, `SupplierStatus`, `CasMetadata`).

### `Site` — `Domain/Suppliers/Site.cs`

`FullAuditedEntity<Guid>`, `IMultiTenant`. A CAS supplier site: `Number` (site code), `PaymentGroup`, address lines, `EmailAddress`, `EFTAdvicePref`, `BankAccount` (masked to the last four digits by `SupplierService.GetSiteEto`), `ProviderId`, `Status`, `SiteProtected`, `LastUpdatedInCas`, `SupplierId`, and `MarkDeletedInUse`.

Unlike `Supplier`, `Site` exposes public setters and two static helpers used by the sync path: `SiteMatchesSiteDto(Site, SiteDto)` (field-by-field equality, used to skip no-op updates) and `UpdateSiteBySiteDto(Site, SiteDto)`.

### `AccountCoding` — `Domain/AccountCodings/AccountCoding.cs`

`AuditedAggregateRoot<Guid>`, `IMultiTenant`. Five GL segments plus an optional `Description` (max 35 chars).

`Create(...)` and `Update(...)` validate each segment's exact length, and require alphanumerics for `ServiceLine`, `Stob` and `ProjectNumber`:

| Segment | Length | Alphanumeric enforced |
|---|---|---|
| `MinistryClient` | 3 | no |
| `Responsibility` | 5 | no |
| `ServiceLine` | 5 | yes |
| `Stob` | 4 | yes |
| `ProjectNumber` | 7 | yes |

Violations throw `BusinessException(ErrorConsts.InvalidAccountCodingField)` with `field` and `length` data. `FullAccountCode()` / `FullAccountCodeWithDescription()` delegate to `AccountCodingFormatter`, which appends the fixed postfix `000000.0000`.

### `PaymentConfiguration` — `Domain/PaymentConfigurations/PaymentConfiguration.cs`

`FullAuditedAggregateRoot<Guid>`, `IMultiTenant`. One row per tenant in practice — every read path takes `GetListAsync()[0]` or `FirstOrDefaultAsync()`; there is no uniqueness constraint enforcing that. Holds `DefaultAccountCodingId` and `PaymentIdPrefix`.

### `PaymentThreshold` — `Domain/UserPaymentThresholds/PaymentThreshold.cs`

`FullAuditedAggregateRoot<Guid>`, `IMultiTenant`. `UserId`, `Threshold`, `Description`. Rows are created lazily by the host's `PaymentSettingsAppService.GetL2ApproversThresholds()`, which inserts a null-threshold row for any user holding the `l2_approver` role that does not have one yet.

### `PaymentTag` — `Domain/PaymentTags/PaymentTag.cs`

`AuditedAggregateRoot<Guid>`, `IMultiTenant`. A join between a `PaymentRequestId` and the host's `Tag` (`Unity.GrantManager.GlobalTag.Tag`) — the same global tag table the application list uses. `PaymentTagSummaryCount` (`Domain/PaymentTags/TagSummaryCount.cs`) is the projection returned by `IPaymentTagRepository.GetTagSummary()`.

## Enums

All in `Unity.Payments.Application.Contracts/Enums/` except where noted.

```csharp
PaymentRequestStatus   L1Pending=1, L1Declined=2, L2Pending=3, L2Declined=4, L3Pending=5,
                       L3Declined=6, Submitted=7, Validated=8, NotValidated=9, Paid=10,
                       Failed=11, FSB=12, HistoricalPayment=13, Cancelled=14
ExpenseApprovalStatus  Requested=1, Approved=2, Declined=3
ExpenseApprovalType    Level1=1, Level2=2, Level3=3
PaymentGroup           EFT=1, Cheque=2          // mapped to CAS "GEN EFT" / "GEN CHQ"
```

`PaymentApprovalAction` (in `Domain/Shared/PaymentsAction.cs`) is the trigger set for the state machine: `None`, `L1Approve`, `L1Decline`, `L2Approve`, `L2Decline`, `L3Approve`, `L3Decline`, `Submit`, `Cancel`.

`CasPaymentRequestStatus` (`Application.Contracts/Codes/`) is a **string constant class**, not an enum. Two of its values are written by Unity (`SentToCas`, `SentToAccountsPayable`); the rest mirror CAS invoice/payment statuses:

| Constant | Value | Meaning |
|---|---|---|
| `SentToCas` | `"SentToCas"` | Unity POSTed the invoice successfully |
| `SentToAccountsPayable` | `"SentToAccountsPayable"` | Unity routed the payment to FSB instead of CAS |
| `ErrorFromCas` | `"Error"` | CAS rejected the invoice |
| `ServiceUnavailable` | `"ServiceUnavailable"` | HTTP-level failure |
| `NotFound` | `"NotFound"` | CAS has no such invoice |
| `NeverValidated` | `"Never Validated"` | Queued for the next CAS run |
| `Validated` | `"Validated"` | Invoice validated |
| `NotPaid` | `"Not Paid"` | CAS ran, not paid |
| `FullyPaid` | `"Fully Paid"` | CAS ran, paid — this is what the rollup counts as paid |
| `Paid`, `Voided`, `Permanent`, `Cancelled` | | other CAS states |

`PaymentsState`, `PaymentApprovalSubState`, `PaymentsAction` and `PaymentsStateGroups` also exist in `Domain/Shared/PaymentsState.cs` and `PaymentsAction.cs`, but nothing references them — see [payments-roadmap.md](payments-roadmap.md#dead-code).

## Persistence

### Schema

`PaymentsDbProperties`: `DbSchema = "Payments"`, `DbTablePrefix = ""`, `ConnectionStringName = "Tenant"`.

| Table (schema `Payments`) | Entity |
|---|---|
| `PaymentRequests` | `PaymentRequest` |
| `ExpenseApprovals` | `ExpenseApproval` |
| `Suppliers` | `Supplier` |
| `Sites` | `Site` |
| `AccountCodings` | `AccountCoding` |
| `PaymentConfigurations` | `PaymentConfiguration` |
| `PaymentThresholds` | `PaymentThreshold` |
| `PaymentTags` | `PaymentTag` |

`ConfigurePayments()` (`EntityFrameworkCore/PaymentsDbContextModelCreatingExtensions.cs`) also re-declares the host's `Tag` entity mapping to `GrantTenant.Tags` so the `PaymentTag → Tag` foreign key resolves from either context.

Indexes on `PaymentRequests`: unique on `ReferenceNumber`; non-unique on `CreationTime`, `Status`, `CorrelationId`, `SiteId`, `AccountCodingId`, `FsbNotificationEmailLogId`; and a filtered composite `(TenantId, CreationTime) WHERE "IsDeleted" = false`.

Delete behaviour is `NoAction` for the `Site`, `AccountCoding` and `Tag` foreign keys — reference data is never cascade-deleted out from under a payment.

### DbContexts and migrations

`PaymentsDbContext` (`EntityFrameworkCore/PaymentsDbContext.cs`, implementing `IPaymentsDbContext`) exposes DbSets for all eight entities and calls `ConfigurePayments()` in `OnModelCreating`. It is registered in `PaymentsApplicationModule` with `AddDefaultRepositories(includeAllEntities: true)`, and the module's custom repositories derive from `EfCoreRepository<PaymentsDbContext, …>`.

`GrantTenantDbContext.OnModelCreating` **also** calls `ConfigurePayments()`. That is the context migrations are generated from — the payments module contains no `Migrations` folder, and the payments tables appear in `GrantTenantDbContextModelSnapshot`.

### Repositories

Custom repositories live in `EntityFrameworkCore/Repositories/`; their interfaces live beside the entities in `Domain/`.

| Interface | Notable members |
|---|---|
| `IPaymentRequestRepository` | `GetCountByCorrelationId`, `GetPaymentRequestCountBySiteId`, `GetPaymentRequestByInvoiceNumber`, `GetTotalPaymentRequestAmountByCorrelationIdAsync`, `GetPaymentRequestsBySentToCasStatusAsync`, `GetPaymentRequestsByFailedsStatusAsync`, `GetPaymentPendingListByCorrelationId(s)Async`, `GetBatchPaymentRollupsByCorrelationIdsAsync` |
| `ISupplierRepository` | `GetBySupplierNumberAsync` |
| `ISiteRepository` | `GetBySupplierAsync` (`IBasicRepository<Site, Guid>`, not the full `IRepository`) |
| `IAccountCodingRepository`, `IPaymentConfigurationRepository`, `IPaymentThresholdRepository` | plain ABP repositories |
| `IPaymentTagRepository` | `GetTagsByPaymentRequestIdAsync`, `GetTagSummary` |

`PaymentsQueryableExtensions.IncludeDetails()` is the module's standard eager-load: `Site → Supplier`, `AccountCoding`, `ExpenseApprovals`.

Three query methods encode business rules worth knowing:

- **`GetTotalPaymentRequestAmountByCorrelationIdAsync`** — sums every payment on an application *except* declined (L1/L2/L3), `Cancelled`, and those whose CAS `InvoiceStatus` is `Cancelled`, `NotFound` or `Error`. This is the "already consumed" figure the create-payment screens subtract from the approved amount.
- **`GetBatchPaymentRollupsByCorrelationIdsAsync`** — one grouped query producing `TotalPaid` (CAS `PaymentStatus` trimmed/upper-cased equals `FULLY PAID`, **or** status is `HistoricalPayment`) and `TotalPending` (any L1/L2/L3 pending, or `Submitted` with no payment status and an invoice status that is empty or not an error/not-found).
- **`GetPaymentRequestsBySentToCasStatusAsync`** — the reconciliation candidate set: `InvoiceStatus` in `ServiceUnavailable`, `SentToCas`, `NotFound`, `SentToAccountsPayable`, `Never Validated`.
- **`GetPaymentRequestsByFailedsStatusAsync`** — the failure-summary set: `InvoiceStatus` in `ServiceUnavailable`, `Error`, modified within the last 24 hours (UTC).

## Domain services

| Manager | Responsibility |
|---|---|
| `PaymentsManager` (`IPaymentsManager`) | Owns the Stateless workflow: `GetActions`, `TriggerAction`, `UpdatePaymentStatusAsync`, `CancelPaymentAsync`, and the two `GetFormPreventPaymentStatus*` lookups. See [payments-approval-workflow.md](payments-approval-workflow.md). |
| `PaymentRequestQueryManager` (`IPaymentRequestQueryManager`) | All read paths for the list page: field-aware eager loading, DTO mapping, user-name hydration via `IExternalUserLookupServiceProvider`, error summaries, rollups, and the manual reconciliation enqueue. |
| `PaymentRequestConfigurationManager` (`IPaymentRequestConfigurationManager`) | Batch names, reference/invoice/sequence number generation, default account coding lookup, and threshold resolution. |
| `InvoiceManager` (`IInvoiceManager`) | Loads the site/supplier/account-coding needed to build a CAS invoice, and writes the CAS response back with concurrency retries. |
| `PermissionCheckerService` (`IPermissionCheckerService`) | Thin `IPermissionChecker` wrapper used by Razor page models. |

`PaymentsWorkflow<TStates, TTriggers>` (`Domain/Workflow/Workflow.cs`) wraps a Stateless `StateMachine` with `GetState()` and `ExecuteActionAsync(trigger)`, which throws `BusinessException("InvalidStateTransition")` when the trigger is not currently permitted. `UnityWorkflowExtensions` adds `GetPermittedActionsAsync()`, `GetAllActions()` and `GetWorkflowDiagram()` (DOT graph).

## Permissions

Declared in `PaymentsPermissionDefinitionProvider` (`Unity.Payments.Application/Permissions/`), with constants in `Unity.Payments.Shared/Permissions/PaymentsPermissions.cs` and `Unity.SharedKernel/Constants/UnitySelector.cs`.

Group `PaymentsPermissions` → permission `PaymentsPermissions.Payments` with children:

| Permission | Display name | Enforced at |
|---|---|---|
| `…Payments.L1ApproveOrDecline` | Approve/Decline L1 Payments | State machine guards, `PaymentRequestAppService.CanPerformLevel1ActionAsync`, approval modal |
| `…Payments.L2ApproveOrDecline` | Approve/Decline L2 Payments | ditto, level 2 |
| `…Payments.L3ApproveOrDecline` | Approve/Decline L3 Payments | ditto, level 3 |
| `…Payments.RequestPayment` | Request Payment | `[Authorize]` on `PaymentRequestAppService.CreateAsync` |
| `…Payments.AddHistoricalPayment` | Add Historical Payment | `[Authorize]` on `CreateHistoricalAsync` |
| `…Payments.CancelPayment` | Cancel Payment | `[Authorize]` on `CancelAsync`, plus the `Cancel` state-machine guard |
| `…Payments.AccountCodingOverride` | Override Account Coding | Create-payments page — enables the batch-wide account coding override |
| `…Payments.EditFormPaymentConfiguration` | Edit Form Payment Configuration | Host's per-form payment configuration widget |

Also in the same group (all `.RequireFeatures("Unity.Payments")`), from `PaymentPermissionGroupDefinitionExtensions.Add_PaymentInfo_Permissions`:

`UnitySelector.Payment.Default` → children `Payment.Summary.Default`, `Payment.Supplier.Default` → `Payment.Supplier.Update`, `Payment.PaymentList.Default`.

And grafted onto the host's `Tags` group: `UnitySelector.Payment.Tags.Create` and `UnitySelector.Payment.Tags.Delete`.

Two constants are declared but never registered or checked: `PaymentsPermissions.Payments.Decline` and `PaymentsPermissions.Payments.EditSupplierInfo` (the supplier widget uses `UnitySelector.Payment.Supplier.Update` instead). See [payments-roadmap.md](payments-roadmap.md#dead-code).

Configuration screens use the host permission `UnitySettingManagementPermissions.ConfigurePayments`, which also acts as the delete policy on `AccountCodingAppService` and the create/delete policy on `PaymentThresholdAppService`.

Roles seeded with payment permissions are defined in `src/Unity.GrantManager.Domain/Permissions/PermissionGrantsDataSeeder.cs` — `l1_approver`, `l2_approver`, `l3_approver` and `financial_analyst` among them.

## Settings

`PaymentSettingsConstants.BackgroundJobs` (`Application.Contracts/Settings/SettingsConstants.cs`) defines two cron keys, both under the `GrantManager.BackgroundJobs.` prefix and both given defaults in the host's `GrantManagerSettingDefinitionProvider`:

| Key | Default | Drives |
|---|---|---|
| `GrantManager.BackgroundJobs.CasPaymentsReconciliation_ProducerExpression` | `0 0 8 1/1 * ? *` (08:00 UTC = midnight PST) | `ReconciliationProducer` |
| `GrantManager.BackgroundJobs.CasFinancialNotificationSummary_ProducerExpression` | `0 0 9 1/1 * ? *` (09:00 UTC = 01:00 PST) | `FinancialNotificationSummaryWorker` |

Both workers fall back to `0 0 9 1/1 * ? *` if the setting cannot be read.

## Error codes

`Domain/Exceptions/ErrorConsts.cs`, all localized in `Unity.Payments.Shared/Localization/Payments/en.json`:

| Code | Message |
|---|---|
| `Unity.Payments:Errors:ZeroPayment` | Cannot submit a payment request for $0.00 |
| `Unity.Payments:Errors:MissingSupplierNumber` | Cannot submit a payment request without a supplier number |
| `Unity.Payments:Errors:MissingSite` | Cannot submit a payment request without a site |
| `Unity.Payments:Errors:MissingAccountCoding` | Cannot submit a payment request without an account coding |
| `Unity.Payments:Errors:ThresholdExceeded` | Batch contains payments needing a third level of approval |
| `Unity.Payments:Errors:L2ApproverRestriction` | You cannot approve payments you already approved as L1 |
| `Unity.Payments:Errors:InvalidAccountCodingFiled` | Invalid account coding field {field} : {length} (note the misspelled key) |
| `Unity.Payments:Errors:ConfigurationExists` / `ConfigurationDoesNotExist` | Payment configuration create/update guards |

Two further codes are raised inline in `PaymentRequestAppService.CancelAsync` without `ErrorConsts` entries: `Payments:PaymentRequestNotFound` and `Payments:CancellationNotAllowed`.
