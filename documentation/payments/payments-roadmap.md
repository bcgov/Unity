# Payments Roadmap — Known Rough Edges

Things worth knowing before extending this module. This is not a backlog; it is the set of behaviours that surprise people reading the code for the first time. If you fix one, delete it from here in the same commit.

## Re-approving a declined payment cannot record the decision

The state machine permits `L1Declined --L1Approve--> L2Pending` and `L2Declined --L2Approve--> L3Pending` (and `L2Declined --Submit--> Submitted`). But `ExpenseApproval.DecisionUserId` and `DecisionDate` are **write-once** — their setters throw `InvalidOperationException` once set, and `ExpenseApproval_Tests.SettingDecisionUserIdTwice_ShouldThrow` asserts that on purpose.

Declining stamps both fields. So when `PaymentsManager.TriggerAction` later calls `.Approve(currentUserId)` on that same slot, the assignment throws. In the batch path (`PaymentRequestAppService.UpdateStatusAsync`) that exception is caught, logged, and the loop continues — so the payment silently stays declined and simply does not appear in the returned list.

Either the workflow should not offer the transition, or the entity needs an explicit "reset decision" operation.

## Unbounded reads

- **`PaymentRequestAppService.GetListAsync` does no server-side paging.** `PaymentsApplicationModule` sets `LimitedResultRequestDto.MaxMaxResultCount` and `ExtensibleLimitedResultRequestDto.MaxMaxResultCount` to `int.MaxValue`, `GetPagedPaymentRequestsWithIncludesAsync` ignores `skipCount`/`maxResultCount` entirely, the count query is commented out (`#pragma warning disable S125`), and the result is `new PagedResultDto<PaymentRequestDto>(paymentWithIncludes.Count, mappedPayments)`. The only thing keeping the query bounded is the date-range filter, which defaults to the last six months in `localStorage` and can be widened by the user.
- **`GetMaxBatchNumberAsync` loads every payment request in the tenant** to compute `max(BatchNumber) + 1`.
- **`GetNextSequenceNumberAsync` loads every payment request in the tenant**, filters to the current year in memory, and parses the sequence out of the most recent `ReferenceNumber`.

Both number generators are also O(n) on a table that only grows.

## Batch and sequence numbers are racy

`GetBatchSetupAsync` reads the max batch number and next sequence number, then the loop assigns `sequence + i` to each payment. Two concurrent submissions read the same values and generate the same reference numbers. The unique index on `PaymentRequests.ReferenceNumber` turns that into an insert failure rather than a duplicate, but the failure surfaces as a rethrown exception mid-batch, leaving earlier payments in the batch already inserted.

## Per-payment exceptions are swallowed in batch updates

`UpdateStatusAsync` wraps each payment in `try { … } catch (Exception ex) { Logger.LogException(ex); }`. A payment that fails to transition is omitted from the returned list with no error surfaced to the user — the modal simply reports success for the ones that worked. This is what hides the declined-payment bug above.

## Level 3 approval rows are created during a GET

`UpdatePaymentRequestStatus.UpdateExpenseApprovalsAsync` inserts or deletes the Level 3 `ExpenseApproval` row while **rendering the approval modal** (`OnGetAsync`), based on whether the amount exceeds the threshold. Opening and closing the modal without approving anything therefore mutates the payment. The decision also duplicates logic that `PaymentRequestAppService.GetLevel2ApprovalActionAsync` re-derives independently when the approval is actually submitted.

## The state machine validates but does not decide

`PaymentsManager.TriggerAction` runs the workflow against a **local copy** of the status purely to check that the transition is permitted, then computes the real destination with a separate `if/else` ladder and assigns it to the entity. The two can drift: the machine says `L2Approve` leads to `L3Pending`, and the ladder independently sets `L3Pending`. `Submit` is the clearest case — the machine always says `Submitted`, the ladder may choose `FSB`.

`ConfigureWorkflow` also calls `Configure` more than once for `L1Pending`, `L2Pending` and `L2Declined`, spreading one state's transitions across separate blocks. Stateless tolerates it, but it makes the table hard to read.

## Dead code

| Symbol | Location | Status |
|---|---|---|
| `PaymentsState`, `PaymentApprovalSubState`, `PaymentsStateGroups.FinalDecisionStates` | `Domain/Shared/PaymentsState.cs` | Never referenced. A parallel status model that was never adopted |
| `PaymentsAction` enum | `Domain/Shared/PaymentsAction.cs` | Never referenced (`PaymentApprovalAction` in the same file is the live one) |
| `PaymentApprovalAction.L3Approve` | `Domain/Shared/PaymentsAction.cs` | Declared but never configured or fired — the L3 approval is `Submit` |
| `PaymentsPermissions.Payments.Decline` | `Unity.Payments.Shared/Permissions/` | Never registered, never checked |
| `PaymentsPermissions.Payments.EditSupplierInfo` | same | Never registered; the supplier widget uses `UnitySelector.Payment.Supplier.Update` |
| `CasClientOptions` | `Integrations/Cas/` | Bound from the `Payments` config section in `GrantManagerApplicationModule`, but `CasTokenService` reads its credentials from `CAS_API_KEY_{CODE}` and `ICasClientCodeLookupService` instead |
| `Supplier.SIN` | `Domain/Suppliers/Supplier.cs` | Mapped and persisted, but `SupplierService.GetEventDtoFromCasResponse` never reads CAS's `sin` field |
| `PaymentRequestDto`'s `explicit operator` | `Application.Contracts/PaymentRequests/` | `public static explicit operator PaymentRequestDto(CreatePaymentRequestDto v) => throw new NotImplementedException();` |

## Exception details are sent to clients

`PaymentsApplicationModule` configures:

```csharp
Configure<AbpExceptionHandlingOptions>(options =>
{
    options.SendExceptionsDetailsToClients = true;
    options.SendStackTraceToClients = false;
});
```

`AbpExceptionHandlingOptions` is global, not per-module, so this affects the whole application, not just payments endpoints.

## Anonymous page conventions

`PaymentsWebModule` marks two Razor Pages anonymous:

```csharp
options.Conventions.AllowAnonymousToPage("/Payments/Index");
options.Conventions.AllowAnonymousToPage("/PaymentConfigurations/Index");
```

`/Payments/Index` does not exist as a page in this module (the list page is `/PaymentRequests/Index`), so that line is inert. `/PaymentConfigurations/Index` does exist; its page model reads the tenant payment configuration with no authorization attribute of its own, relying on the surrounding host route and the `ConfigurePayments` check on the Configuration Management page that links to it.

`InvoiceService` and `SupplierService` also carry `[AllowAnonymous]`, mitigated by `[RemoteService(false)]` — they are not exposed as HTTP endpoints.

## Project layout diverges from every other module

There are no `Unity.Payments.Domain`, `Unity.Payments.Domain.Shared`, or `Unity.Payments.EntityFrameworkCore` projects. The domain model lives in `Unity.Payments.Application/Domain/` and persistence in `Unity.Payments.Application/EntityFrameworkCore/`, so nothing structurally prevents a domain entity from depending on an application service. `InvoiceManager` (a `DomainService`) already injects `PaymentConfigurationAppService` directly, and `PermissionCheckerService` — a `PaymentsAppService` — lives under `Domain/Shared/`.

Two dbcontexts also map the same tables: `PaymentsDbContext` (bound to the `Tenant` connection string, used by the module's custom repositories) and `GrantTenantDbContext` (which calls `ConfigurePayments()` and is the context migrations are generated from). Both register default repositories for the payments entities.

## Naming and file-placement oddities

- `PaymentQueueService` lives in `Unity.Payments.Application/Integrations/RabbitMQ/` but is declared in namespace **`Unity.Notifications.Integrations.RabbitMQ`**.
- `Domain/UserPaymentThresholds/IPaymentConfigurationRepository.cs` declares `IPaymentThresholdRepository`, and its implementation is `EntityFrameworkCore/Repositories/UserPaymentThresholdRepository.cs`.
- `Pages/PaymentRequests/Index.cshtml.cs` declares `PaymentsPageModel` in namespace `Unity.Payments.Web.Pages.Payments`, not a `PaymentRequests` namespace.
- The error-code constant `Unity.Payments:Errors:InvalidAccountCodingFiled` is misspelled, in both `ErrorConsts` and `en.json`.
- The Payments main-menu item's display name is the hard-coded string `"Payments"` rather than a `PaymentsResource` key, so it does not localize.

## CAS reconciliation details

- **`"NotFound2"` is a magic string.** When a payment already sitting at `InvoiceStatus = NotFound` comes back `NotFound` again, `UpdatePaymentRequestFromCasResult` writes the literal `NotFound + "2"`. Nothing else in the codebase reads that value; it exists as a human signal in the list.
- **FSB payments are polled against CAS.** `SentToAccountsPayable` is in `ReCheckStatusList`, so every FSB payment is enqueued for reconciliation nightly and will normally return `NotFound` until Financial Services Branch keys it in.
- **In-process retry backoff.** `InvoiceManager.UpdatePaymentRequestWithInvoiceAsync` retries concurrency conflicts three times with `await Task.Delay(75)` between attempts, blocking the consumer thread.
- **Enqueue failures are silent.** `AddPaymentRequestsToInvoiceQueue` catches and logs everything; a broker outage leaves the payment at `Submitted` with no `InvoiceStatus` and nothing scheduled to retry it (reconciliation only picks up payments that already have a re-check `InvoiceStatus`).

## Convention drift from the repo's own C# rules

`.claude/rules/csharp.md` requires `GuidGenerator.Create()` over `Guid.NewGuid()` and `Clock.Now` over `DateTime.UtcNow`. Payments uses the raw APIs throughout — `new PaymentRequest(Guid.NewGuid(), …)` in `PaymentRequestAppService`, `new Supplier(Guid.NewGuid(), …)` in `SupplierAppService`, `new ExpenseApproval(Guid.NewGuid(), …)` in the approval modal, `DateTime.UtcNow` in `ExpenseApproval.Approve`/`Decline`, `InvoiceService`, and `PaymentRequestConfigurationManager`. `PaymentsManager.CancelPaymentAsync` is the exception: it uses `Clock.Now`.

`PaymentRequest.PaymentTags` is also explicitly set to `null` in both public constructors, so the collection is null on a freshly created aggregate.

## Test coverage

The module ships `Unity.Payments.TestBase` and `Unity.Payments.Application.Tests` only — there is no `Domain.Tests` or `EntityFrameworkCore.Tests` project. Existing coverage:

| Area | Tests |
|---|---|
| `PaymentRequest` construction and validation | `PaymentRequest_Constructor_Tests` |
| `ExpenseApproval` write-once semantics | `ExpenseApproval_Tests` |
| Payment rollups | `PaymentRequestQueryManager_PaymentRollup_Tests`, `PaymentRequestRepository_PaymentRollup_Tests` |
| Repository queries | `PaymentRequestRepository_Tests` |
| Account coding validation | `AccountCoding_Tests` |
| App service basics | `PaymentRequestAppService_Tests`, `SampleManager_Tests` |
| PaymentInfo widget | `PaymentInfoViewComponentTests` |

Untested: the whole `PaymentsManager` state machine, `DetermineTriggerActionAsync` level selection and separation of duties, threshold resolution, the CAS clients and consumers, the supplier/site sync, the FSB notifier, and the parent/child amount validation in `PaymentRequestPageHelperService`.
