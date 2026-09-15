# Payments — CAS Integration

**CAS** is the BC government's Corporate Accounting System. Unity talks to its ORDS REST API for three things: creating AP invoices, reading invoice/payment status, and reading supplier master data. Supplier reads are documented separately in [payments-suppliers-and-sites.md](payments-suppliers-and-sites.md); this document covers the invoice half.

Nothing calls CAS synchronously from a request thread. Approval enqueues; a RabbitMQ consumer calls CAS.

## Authentication

`CasTokenService` (`Integrations/Cas/CasTokenService.cs`, `ICasTokenService`) resolves a bearer token **per tenant**:

```text
casBaseUrl   = IEndpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.PAYMENT_API_BASE)
casClientCode= tenant.ExtraProperties["CasClientCode"]        → UserFriendlyException if absent
casClientId  = ICasClientCodeLookupService.GetClientIdByCasClientCodeAsync(casClientCode)
clientSecret = configuration["CAS_API_KEY_{CASCLIENTCODE}"]   (upper-cased code)
token        = TokenService.GetAuthTokenAsync({ Url = "{casBaseUrl}/oauth/token", ClientId, ClientSecret, ApiKey = "CasApiKey" })
```

The token is cached in `IDistributedCache<TokenValidationResponse, string>` — the same cache type the Notifications module uses for CHES. `PAYMENT_API_BASE` is a `DynamicUrl` row seeded by `DynamicUrlDataSeeder` ("BC Corporate Accounting Services API"), so the endpoint is changed in data, not in configuration files.

The tenant's `CasClientCode` is set through Tenant Management — see [`tenant-management/`](../tenant-management/README.md).

`CasClientOptions` is bound from the `Payments` configuration section in `GrantManagerApplicationModule`, but `CasTokenService` reads its credentials from the per-code sources above rather than from those options.

## Invoice creation

### 1. Enqueue

When `PaymentsManager.TriggerAction` fires `Submit` on a form that does **not** prevent payment, it calls `CasPaymentRequestCoordinator.AddPaymentRequestsToInvoiceQueue(paymentRequest)`, which publishes an `InvoiceMessages` envelope:

```csharp
new InvoiceMessages {
    TimeToLive      = TimeSpan.FromMinutes(10),
    PaymentRequestId, InvoiceNumber, SupplierNumber,
    SiteNumber      = paymentRequest.Site?.Number ?? string.Empty,
    TenantId        = currentTenant.Id
}
```

Exceptions are caught and logged — a broker outage does not roll back the approval, but the payment then sits at `Submitted` with no CAS record until reconciliation or manual intervention picks it up.

### 2. Consume

`InvoiceConsumer : IQueueConsumer<InvoiceMessages>` is registered in `PaymentsApplicationModule` via `AddQueueMessageConsumer<InvoiceConsumer, InvoiceMessages>()`. Tenant context and audit scope are established by the shared `QueueConsumerHandler` before `ConsumeAsync` runs, so the consumer itself only guards against empty invoice numbers and empty tenant ids, then calls `InvoiceService.CreateInvoiceByPaymentRequestAsync(invoiceNumber)`.

### 3. Build and POST the invoice

`InvoiceService.CreateInvoiceByPaymentRequestAsync`:

1. `InvoiceManager.GetPaymentRequestDataAsync(invoiceNumber)` — finds the payment by invoice number, requires an `AccountCodingId`, loads the `AccountCoding`, and formats the GL distribution string via `PaymentConfigurationAppService.GetAccountDistributionCode`.
2. `InitializeCASInvoice(paymentRequest, accountDistributionCode)` — returns `null` (silently skipping the send) if the site, its supplier, the supplier number, or the distribution code is missing.
3. `CreateInvoiceAsync(invoice)` — serializes with `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`, `POST {casBaseUrl}/cfs/apinvoice/` through `IResilientHttpRequest`.
4. `InvoiceManager.UpdatePaymentRequestWithInvoiceAsync(paymentRequestId, invoiceResponse)` — writes the result back.

Any exception in this method is caught and logged; an empty `InvoiceResponse` is returned. The failure surfaces on the payment only if step 4 ran.

#### The invoice payload

`Invoice` (`Application.Contracts/Integrations/Cas/Invoice.cs`) is the CAS wire type. `InitializeCASInvoice` fills:

| Field | Source |
|---|---|
| `SupplierNumber`, `SupplierName` | `site.Supplier` |
| `SupplierSiteNumber` | `site.Number` |
| `PayGroup` | `site.PaymentGroup` mapped to `"GEN EFT"` / `"GEN CHQ"`; an unmapped value throws `UserFriendlyException` |
| `InvoiceNumber` | `paymentRequest.InvoiceNumber` |
| `InvoiceDate`, `DateInvoiceReceived`, `GlDate` | today in **Pacific Standard Time**, formatted `dd-MMM-yyyy` — deliberately not UTC, because CAS rejects future-dated invoices |
| `InvoiceAmount` | `paymentRequest.Amount` |
| `InvoiceBatchName` | `paymentRequest.BatchName` |
| `PaymentAdviceComments` | `paymentRequest.Description` trimmed to 50 chars, or `"{TenantName} - Grant Payment"` (also capped at 50) when the description is blank |
| `QualifiedReceiver` | the Level 1 approver's `"{Name} {Surname}"` (capped at 150 chars), falling back to their username, then to empty. Resolution failures are logged as a warning and yield empty |
| `InvoiceLineDetails` | one line: number 1, the full amount, `DefaultDistributionAccount` = the account distribution code |

#### The account distribution code

`PaymentConfigurationAppService.GetAccountDistributionCode(accountCoding)` and `AccountCodingFormatter.Format` both produce:

```text
{MinistryClient}.{Responsibility}.{ServiceLine}.{Stob}.{ProjectNumber}.000000.0000
```

for example `039.15006.10120.5185.1500000.000000.0000`. If any of the five segments is null the formatter returns an empty string, which makes `InitializeCASInvoice` skip the send.

### 4. Record the response

`InvoiceManager.UpdatePaymentRequestWithInvoiceAsync` retries up to **3 times**, each attempt in a fresh unit of work, catching `AbpDbConcurrencyException` / `DbUpdateConcurrencyException` with a 75 ms pause between attempts. On each attempt it:

- returns early if the payment already has `InvoiceStatus == SentToCas` (idempotency guard against double-consume),
- sets `CasHttpStatusCode` and `CasResponse` from the response,
- sets `InvoiceStatus` to `SentToCas` when `invoiceResponse.IsSuccess()`, otherwise `Error`.

Exhausting the retries throws `UserFriendlyException`; any other exception is wrapped in one.

## Reconciliation

### The nightly sweep

`ReconciliationProducer` is a `[DisallowConcurrentExecution]` `QuartzBackgroundWorkerBase`. Its cron comes from `PaymentSettingsConstants.BackgroundJobs.CasPaymentsReconciliation_ProducerExpression` (default `0 0 8 1/1 * ? *` — 08:00 UTC, midnight Pacific), falling back to `0 0 9 1/1 * ? *` if the setting cannot be read.

`CasPaymentRequestCoordinator.AddPaymentRequestsToReconciliationQueue()` iterates **every tenant**, changes into each tenant's context, and enqueues a `ReconcilePaymentMessages` (TTL 10 minutes) for each payment returned by `GetPaymentRequestsBySentToCasStatusAsync()` — i.e. every payment whose CAS `InvoiceStatus` is one of:

```text
ServiceUnavailable · SentToCas · NotFound · SentToAccountsPayable · Never Validated
```

Note that `SentToAccountsPayable` (the FSB route) is in the re-check list, so FSB payments are also polled against CAS — they will normally come back `NotFound` until FSB keys them in.

### Manual re-check

Staff can select rows on the Payments list and press **Check Status**, which calls `PaymentRequestAppService.ManuallyAddPaymentRequestsToReconciliationQueue(ids)` → `PaymentRequestQueryManager.ManuallyAddPaymentRequestsToReconciliationQueueAsync` → `CasPaymentRequestCoordinator.ManuallyAddPaymentRequestsToReconciliationQueue`. Same message, same consumer; the only difference is that the site is loaded explicitly onto the DTO first.

### The consumer

`ReconciliationConsumer : IQueueConsumer<ReconcilePaymentMessages>`:

```text
InvoiceService.GetCasPaymentAsync(tenantId, invoiceNumber, supplierNumber, siteNumber)
    GET {casBaseUrl}/cfs/apinvoice/{invoiceNumber}/{supplierNumber}/{siteNumber}
        ↓
if result.InvoiceStatus is non-empty
        ↓
CasPaymentRequestCoordinator.UpdatePaymentRequestStatus(tenantId, paymentRequestId, result)
```

`GetCasPaymentAsync` returns an empty `CasPaymentSearchResult` on a failed response, except that it stamps `InvoiceStatus` with the HTTP status code string when a response came back but was not successful — which is how values such as `"ServiceUnavailable"` end up on the payment.

`UpdatePaymentRequestStatus` opens its own transactional unit of work (`requiresNew: true`) and applies `UpdatePaymentRequestFromCasResult`:

- If the payment **already** has `InvoiceStatus == NotFound` and CAS returns `NotFound` again, the incoming status is rewritten to `"NotFound2"` — a marker for "we have now looked twice and it is still not there".
- `InvoiceStatus`, `PaymentStatus`, `PaymentDate` (normalised from `dd-MMM-yyyy` to `yyyy-MM-dd`), `PaymentNumber` are all set from the CAS result.
- When CAS returned any invoice status, `CasHttpStatusCode` is set to `200` and `CasResponse` to `"SUCCEEDED"`.

The unit of work is completed inside the method so the entity changes are collected into the audit log that `QueueConsumerHandler` persists after `ConsumeAsync` returns.

## What the statuses mean together

`PaymentRequest.Status` and the CAS fields move independently. A typical successful life cycle:

| Step | `Status` | `InvoiceStatus` | `PaymentStatus` |
|---|---|---|---|
| Created | `L1Pending` | null | null |
| L1 approved | `L2Pending` | null | null |
| L2 approved (over threshold) | `L3Pending` | null | null |
| Submitted | `Submitted` | null | null |
| Invoice POSTed | `Submitted` | `SentToCas` | null |
| First recon | `Submitted` | `Never Validated` | null |
| CAS validates | `Submitted` | `Validated` | `Not Paid` |
| CAS pays | `Submitted` | `Validated` | `Fully Paid` |

The rollup counts a payment as **paid** when `PaymentStatus` (trimmed, case-insensitive) is `Fully Paid`, or when `Status` is `HistoricalPayment`. `Status` itself is never advanced to `Paid` by the reconciliation path.

The FSB variant:

| Step | `Status` | `InvoiceStatus` |
|---|---|---|
| Final approval on a `PreventPayment` form | `FSB` | `SentToAccountsPayable` |
| FSB notification email sent | `FSB` | `SentToAccountsPayable`, plus `FsbApNotified = "Yes"` |

## Error surfacing

`PaymentRequestQueryManager.ApplyErrorSummary` copies `CasResponse` into the DTO's `ErrorSummary` whenever it is non-empty and not `"SUCCEEDED"`. The Payments list renders that as a clickable CAS Response cell; `Pages/PaymentRequests/CasPaymentRequestResponse.cshtml` displays the full text, HTML-encoded with `;` turned into line breaks.

Payments whose `InvoiceStatus` is `ServiceUnavailable` or `Error` and that were modified in the last 24 hours are also collected nightly into the failed-payment summary email — see [payments-notifications.md](payments-notifications.md#the-nightly-failed-payment-summary).
