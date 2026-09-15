# Payments Overview

## What problem it solves

A grant is only real when the money moves. Once an application reaches `GRANT_APPROVED` with an approved amount, someone has to:

1. Decide **how much of that approved amount to pay now** (grants are often paid in instalments, and parent/child application hierarchies share one approved envelope).
2. Get that amount **signed off** by one, two, or three levels of expense authority, where the number of levels depends on the amount and on per-form and per-user thresholds.
3. Turn it into an **AP invoice in CAS**, the province's Corporate Accounting System, addressed to a supplier number and site code that CAS recognises, coded to a GL distribution string the ministry recognises.
4. **Watch CAS** until the invoice is validated and paid, and surface failures to staff.

Some programs are not permitted to submit directly to CAS. For those, the same approval workflow runs but the last step is replaced: the batch is emailed as a spreadsheet to the Financial Services Branch AP team, who key it in themselves.

Unity.Payments owns all of that, plus the reference data it needs — suppliers, sites, account codings, thresholds — and the staff-facing Payments list where the whole portfolio is managed.

## The four big architectural facts

### 1. A payment request is a state machine, and the state machine is a permission gate

`PaymentRequest.Status` (`PaymentRequestStatus`) is the spine of the module. Transitions are declared with the **Stateless** library in `PaymentsManager.ConfigureWorkflow`, and every transition is guarded by `PermitIfAsync(... HasPermissionAsync(...))` — so the same object answers both "is this transition legal from here?" and "is *this user* allowed to make it?".

```text
L1Pending ──L1Approve──▶ L2Pending ──L2Approve──▶ L3Pending ──Submit──▶ Submitted (or FSB)
    │                        │                        │
    │ L1Decline              │ L2Decline              │ L3Decline
    ▼                        ▼                        ▼
L1Declined              L2Declined              L3Declined
    │                        │
    └──L1Approve──▶ L2Pending└──L2Approve──▶ L3Pending / ──Submit──▶ Submitted

Cancel is permitted from L1Pending, L2Pending, L3Pending and HistoricalPayment.
```

The `Submit` trigger is the interesting one: it is permitted from **L2Pending** (when the amount is below the applicable threshold, so no L3 is needed) and from **L3Pending**. Where `Submit` lands depends on the form: if the application's `ApplicationForm.PreventPayment` is true the request goes to `FSB`, otherwise to `Submitted` and onto the CAS invoice queue. See [payments-approval-workflow.md](payments-approval-workflow.md).

### 2. Nothing calls CAS from a request thread

Approving the last level does not call CAS. It enqueues:

```text
PaymentsManager.TriggerAction (Submit)
        ↓
CasPaymentRequestCoordinator.AddPaymentRequestsToInvoiceQueue
        ↓
PaymentQueueService  ──▶ RabbitMQ (InvoiceMessages, TTL 10 min)
        ↓
InvoiceConsumer      ──▶ InvoiceService.CreateInvoiceByPaymentRequestAsync
        ↓
CAS POST cfs/apinvoice   →  InvoiceManager.UpdatePaymentRequestWithInvoiceAsync
                            sets InvoiceStatus = SentToCas | Error
```

A separate nightly Quartz worker (`ReconciliationProducer`) sweeps every tenant for payments still in a "re-check" CAS status and enqueues `ReconcilePaymentMessages`; `ReconciliationConsumer` GETs the invoice from CAS and writes `InvoiceStatus` / `PaymentStatus` / `PaymentNumber` / `PaymentDate` back. See [payments-cas-integration.md](payments-cas-integration.md).

### 3. Supplier and site data is a mirror of CAS, refreshed opportunistically

Unity never invents a supplier. `SupplierService` fetches supplier JSON from CAS by supplier number or BN9, converts it to an `UpsertSupplierEto`, and publishes it on the local event bus. `UpsertSupplierHandler` creates or updates the `Supplier` and its `Site` rows, then publishes `ApplicantSupplierEto` so the host can link the applicant. The Supplier Info widget re-checks CAS whenever it lists sites and silently re-syncs if anything differs. See [payments-suppliers-and-sites.md](payments-suppliers-and-sites.md).

### 4. The module depends on the host

Unlike Flex or TenantManagement, `Unity.Payments.Application` references `Unity.GrantManager` types directly — `IApplicationRepository`, `IApplicationFormRepository`, `IGrantApplicationAppService`, `IApplicationLinksService`, `IEndpointManagementAppService`, `PaymentConsts` — and `Unity.Notifications` types (`EmailNotificationEvent`, `IEmailGroupsRepository`, `NotificationsSettings`). The dependency is not one-way: the host also references payments types (`PaymentThreshold`, `AccountCoding`, `IPaymentRequestAppService`, `PaymentGroup`). Treat Payments and the core GrantManager application as mutually coupled, not as a cleanly separable module.

## Module layout and dependency direction

```text
Unity.Payments.Shared                      → PaymentsResource localization, PaymentsPermissions constants,
                                             HtmlTable helper (used to build the failed-payment email body)
        ↑
Unity.Payments.Application.Contracts       → DTOs, I*AppService interfaces, enums, CAS wire types
                                             (Invoice, InvoiceResponse, CasPaymentSearchResult),
                                             CasPaymentRequestStatus codes, PaymentSettingsConstants
        ↑
Unity.Payments.Application                 → Domain/          entities, value objects, domain managers,
                                             │                 repository interfaces, workflow, exceptions
                                             ├ EntityFrameworkCore/  PaymentsDbContext, ConfigurePayments(),
                                             │                 custom repositories
                                             ├ Integrations/   CAS clients, RabbitMQ producer + consumers
                                             ├ PaymentRequests/ app service, coordinator, cache, FSB notifier,
                                             │                 Quartz workers
                                             └ Suppliers/, PaymentTags/, PaymentConfigurations/, PaymentInfo/,
                                               Handlers/, Permissions/
        ↑
Unity.Payments.Web                         → Razor Pages, view components (PaymentInfo, SupplierInfo,
                                             PaymentActionBar), menu contributor, page helper service
```

The host wires the module in at four points:

- `GrantManagerDomainSharedModule` → `[DependsOn(typeof(PaymentsSharedModule))]`
- `GrantManagerApplicationModule` → `[DependsOn(typeof(PaymentsApplicationModule))]`, and binds `CasClientOptions` from the `Payments` configuration section
- `GrantManagerWebModule` → `[DependsOn(typeof(PaymentsWebModule))]`, and re-registers the module's conventional controllers
- `GrantTenantDbContext.OnModelCreating` → `modelBuilder.ConfigurePayments()`

### Two DbContexts over the same tables

`PaymentsDbContext` is a real, registered `AbpDbContext` (`AddAbpDbContext<PaymentsDbContext>(options => options.AddDefaultRepositories(includeAllEntities: true))`), and the module's custom repositories are `EfCoreRepository<PaymentsDbContext, …>`. Its connection-string name is `Tenant` (`PaymentsDbProperties.ConnectionStringName`), so it points at the same per-tenant database as `GrantTenantDbContext`. `GrantTenantDbContext` also calls `ConfigurePayments()`, which is what puts the `Payments` schema tables into the tenant migration model — the module ships **no migrations of its own**. Schema changes to payments entities are added as tenant migrations:

```bash
cd applications/Unity.GrantManager/src/Unity.GrantManager.EntityFrameworkCore
dotnet ef migrations add <Name> --context GrantTenantDbContext --output-dir Migrations/TenantMigrations
```

## Feature and permission gating

One **tenant feature** gates the whole module: `Unity.Payments` ("Payments"), defined in the host's `GrantManagerFeaturesDefinitionProvider` with `defaultValue: "false"`. It is enforced in four ways:

| Where | How |
|---|---|
| App services | `[RequiresFeature("Unity.Payments")]` on `PaymentRequestAppService`, `PaymentConfigurationAppService`, `PaymentInfoAppService`, `PaymentTagAppService`, `SupplierAppService`, `SiteAppService`, and the host's `ApplicantPaymentsAppService` |
| Main menu | `PaymentsMenuContributor` checks `IFeatureChecker.IsEnabledAsync("Unity.Payments")` before adding the Payments item |
| Widgets | `PaymentInfoViewComponent` and `SupplierInfoViewComponent` return an empty view model when the feature is off |
| Permissions | Every `UnitySelector.Payment.*` permission is declared `.RequireFeatures("Unity.Payments")`, as are the two payment tag permissions |

Permissions come from two families, both declared in `PaymentsPermissionDefinitionProvider`:

- **`PaymentsPermissions.Payments.*`** — the workflow permissions (`L1ApproveOrDecline`, `L2ApproveOrDecline`, `L3ApproveOrDecline`, `RequestPayment`, `AccountCodingOverride`, `AddHistoricalPayment`, `CancelPayment`, `EditFormPaymentConfiguration`) under the `PaymentsPermissions` group.
- **`UnitySelector.Payment.*`** — the granular UI-surface permissions (`Payment.Default`, `Payment.Summary`, `Payment.Supplier` + `.Update`, `Payment.PaymentList`) grafted onto the same group, plus `UnitySelector.Payment.Tags.Create` / `.Delete` grafted onto the host's existing `Tags` group.

Configuration screens are gated separately by the host permission `UnitySettingManagementPermissions.ConfigurePayments`. See [payments-domain-model.md](payments-domain-model.md#permissions).

## External dependencies

| System | Used for | Reached through |
|---|---|---|
| **CAS** (Corporate Accounting System) | Creating AP invoices, reading invoice/payment status, reading supplier and site master data | `InvoiceService` and `SupplierService` over `IResilientHttpRequest`; base URL from the `DynamicUrl` row `PAYMENT_API_BASE` via the host's `IEndpointManagementAppService` |
| **CAS OAuth** | Bearer tokens for the above | `CasTokenService` — client id resolved from the tenant's `CasClientCode` extra property via `ICasClientCodeLookupService`; secret read from configuration key `CAS_API_KEY_{CODE}`; token cached in `IDistributedCache<TokenValidationResponse, string>` |
| **RabbitMQ** | Decoupling invoice creation and reconciliation from the request thread | `PaymentQueueService` (producer) + `InvoiceConsumer` / `ReconciliationConsumer`, registered with `AddQueueMessageConsumer<,>` in `PaymentsApplicationModule` |
| **CHES** (via `Unity.Notifications`) | FSB spreadsheet notifications and the nightly failed-payment summary | Publishing `EmailNotificationEvent` on the local event bus; the Notifications module owns the rest of the pipeline |
| **Redis / distributed cache** | Holding the selected payment-request ids between the list page and a modal | `PaymentIdsCacheService` (`IDistributedCache<List<Guid>, string>`, 5-minute TTL) |
| **Quartz** | The nightly reconciliation sweep and failed-payment summary | `ReconciliationProducer`, `FinancialNotificationSummaryWorker` — both `QuartzBackgroundWorkerBase`, cron read from settings |

## Core concepts (glossary)

| Term | Meaning |
|---|---|
| **`PaymentRequest`** | One payment. Aggregate root. Carries the amount, the payee/supplier/site snapshot, the batch it belongs to, its `Status`, its CAS response fields, its `ExpenseApprovals`, tags, notes, FSB tracking, and cancellation tracking. |
| **`PaymentRequestStatus`** | The Unity-side state: `L1Pending`, `L1Declined`, `L2Pending`, `L2Declined`, `L3Pending`, `L3Declined`, `Submitted`, `Validated`, `NotValidated`, `Paid`, `Failed`, `FSB`, `HistoricalPayment`, `Cancelled`. |
| **`InvoiceStatus` / `PaymentStatus`** | The CAS-side state, stored as free strings from `CasPaymentRequestStatus` (`SentToCas`, `SentToAccountsPayable`, `Error`, `NotFound`, `Never Validated`, `Validated`, `Not Paid`, `Fully Paid`, `Cancelled`, …). Distinct from `Status` — a payment can be `Submitted` in Unity and `Never Validated` in CAS. |
| **`ExpenseApproval`** | One approval slot on a payment: `Type` (`Level1`/`Level2`/`Level3`) and `Status` (`Requested`/`Approved`/`Declined`), plus a write-once `DecisionUserId` and `DecisionDate`. Level 1 and 2 are created with the payment; Level 3 is added or removed on demand when the approval modal decides the amount crosses the threshold. |
| **Threshold** | The dollar figure that decides whether L3 approval is required. Resolved as `min(form.PaymentApprovalThreshold, user.PaymentThreshold)` when both exist, else whichever exists, else `0`. |
| **`Supplier` / `Site`** | The CAS payee. A supplier has a number and a name; a site has a site code, an address, a `PaymentGroup` (EFT or Cheque) and bank details. CAS addresses a payment by supplier number **plus** site code. |
| **`AccountCoding`** | The five GL segments — `MinistryClient` (3), `Responsibility` (5), `ServiceLine` (5), `Stob` (4), `ProjectNumber` (7) — formatted by `AccountCodingFormatter` into the CAS `defaultDistributionAccount` string `client.resp.service.stob.project.000000.0000`. |
| **`PaymentConfiguration`** | One row per tenant: the default account coding and the `PaymentIdPrefix` used to build batch names, reference numbers, and invoice numbers. |
| **Batch** | Payments created together in one submission share a `BatchName` (`{prefix}_UNITY_BATCH_{n}`) and `BatchNumber`. FSB notification emails are grouped and titled by batch name. |
| **Reference number / invoice number** | `{prefix}-{year}-{seq:D4}` and `{prefix}-{year}-{originalInvoiceNumber}-{seq:D4}`. `ReferenceNumber` is uniquely indexed and is the "Payment ID" shown in the UI. |
| **`PreventPayment`** | A per-form flag. When set, an approved payment goes to `FSB` and is emailed to Financial Services Branch instead of being submitted to CAS. |
| **Historical payment** | A payment recorded after the fact for reconciliation of pre-Unity spending. Created directly in `HistoricalPayment` status with `PaymentStatus`/`InvoiceStatus` = `Paid`, no expense approvals, and never sent to CAS. |
| **Recon** | The nightly re-check of CAS status for payments in a re-check state. Staff can also trigger it for selected rows from the list page ("Check Status"). |
| **Payment rollup** | `TotalPaid` + `TotalPending` for an application *and its linked child applications*, used to compute how much of the approved amount is still available. |

## Read in this order

1. **[payments-overview.md](payments-overview.md)** (this file).
2. **[payments-domain-model.md](payments-domain-model.md)** — entities, schema, permissions, settings.
3. **[payments-approval-workflow.md](payments-approval-workflow.md)** — the state machine end to end.
4. **[payments-cas-integration.md](payments-cas-integration.md)** — invoice creation and reconciliation.
5. **[payments-suppliers-and-sites.md](payments-suppliers-and-sites.md)** — the CAS payee mirror.
6. **[payments-notifications.md](payments-notifications.md)** — FSB spreadsheets and failure summaries.
7. **[payments-web-ui.md](payments-web-ui.md)** — pages, modals, widgets, gating.
8. **[payments-roadmap.md](payments-roadmap.md)** — known rough edges.
