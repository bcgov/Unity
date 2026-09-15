# Payments — Notifications

Payments sends two kinds of email, both through `Unity.Notifications`. It never talks to CHES directly: it publishes an `EmailNotificationEvent` on the local event bus and the Notifications pipeline takes over — see [`notifications/notifications-email-pipeline.md`](../notifications/notifications-email-pipeline.md).

| Trigger | Action | Recipients | Content |
|---|---|---|---|
| Payments reach `FSB` status during an approval batch | `EmailAction.SendFsbNotification` | the **FSB-AP** email group | one email per batch, with an `.xlsx` attachment listing the payments |
| Nightly Quartz job finds recently failed CAS payments | `EmailAction.SendFailedSummary` | every `IEmailRecipientStrategy`'s recipients (currently the **Payments** email group) | an HTML table of failed payments |

A third path exists indirectly: `PaymentStatusChangedEvent` is published on create, on every status change, and on cancel, and the host's `Events/ScheduledNotificationEventHandler` can match it against a configured `ScheduledNotification` to send a templated email to the applicant. That configuration is documented in [`notifications/notifications-scheduled-notifications.md`](../notifications/notifications-scheduled-notifications.md).

## Recipient strategies

`IEmailRecipientStrategy` (`PaymentRequests/Notifications/IEmailRecipientStrategy.cs`) is a one-method strategy — `Task<List<string>> GetEmailRecipientsAsync()` plus a `StrategyName` — extending `ISingletonDependency`.

| Implementation | Group | Registered how |
|---|---|---|
| `PaymentsEmailGroupStrategy` | `"Payments"` | Explicitly, as a singleton, in `PaymentsApplicationModule`: `AddSingleton<IEmailRecipientStrategy, PaymentsEmailGroupStrategy>()` |
| `FsbApEmailGroupStrategy` | `"FSB-AP"` | Also a singleton, but it deliberately **does not implement `IEmailRecipientStrategy`** — so it is never picked up by the `IEnumerable<IEmailRecipientStrategy>` injection used by the failure summary, and is only used by the FSB notifier |

Both read the same way: find the email group by case-insensitive name in `IEmailGroupsRepository`, read its members from `IEmailGroupUsersRepository`, then resolve addresses via `IIdentityUserRepository.GetListByIdsAsync`. A missing group, an empty group, or a member with no email address is logged as a warning and yields fewer (or zero) recipients rather than an exception. Both groups are seeded per tenant by `NotificationsDataSeedContributor`.

## FSB payment notifications

### When it fires

`PaymentRequestAppService.UpdateStatusAsync` tracks which payments transitioned **into** `FSB` during the batch (previous status was not `FSB`). After the approval loop, if that list is non-empty it reloads those payments with details and calls `FsbPaymentNotifier.NotifyFsbPayments(fsbPayments)`. Exceptions there are logged and swallowed — an email failure must not undo an approval.

### One email per batch

`FsbPaymentNotifier` (`ISingletonDependency`):

1. Resolves recipients from `FsbApEmailGroupStrategy`. **No recipients → nothing is sent**, only a warning is logged.
2. Groups the payments by `BatchName`, treating null/blank as `"Unknown"`.
3. Resolves the tenant name (for the email body) and the from-address (`NotificationsSettings.Mailing.DefaultFromAddress`, falling back to `NoReply@gov.bc.ca`).
4. Sends one email per batch group via `SendBatchNotification`, counting successes and failures. A failure on one batch is logged and processing continues with the rest; only a failure of the whole grouping operation throws.

`SendBatchNotification` builds the payment data, generates the spreadsheet, and publishes:

```csharp
new EmailNotificationEvent {
    Action            = EmailAction.SendFsbNotification,
    TenantId          = currentTenant.Id,
    Subject           = batchName,                 // the email subject IS the batch name
    Body              = GenerateEmailBody(tenantName),
    EmailFrom         = fromAddress,
    EmailAddressList  = recipients,
    ApplicationId     = Guid.Empty,                // batch-level, not application-level
    EmailAttachments  = [ { FileName, Content = excelBytes, ContentType = xlsx } ],
    PaymentRequestIds = batchPayments.Select(p => p.Id)
}
```

The attachment filename is `FSB_Payments_{sanitizedBatchName}_{yyyyMMdd_HHmmssfff}.xlsx`. `SanitizeFileName` replaces `Path.GetInvalidFileNameChars()` and spaces with underscores and truncates the batch name to 217 characters.

The body is a fixed HTML message naming the tenant, ending with a do-not-reply notice.

### The spreadsheet

`FsbPaymentExcelGenerator.GenerateExcelFile` (ClosedXML) writes a single `FSB Payments` worksheet with 20 columns, populated from `FsbPaymentData`:

| # | Column | Source |
|---|---|---|
| 1 | Batch # | `payment.BatchName` |
| 2 | Contract Number | `payment.ContractNumber` |
| 3 | Payee Name | `payment.PayeeName` |
| 4 | CAS Supplier/Site Number | `{site.Supplier.Number}/{site.Number}` |
| 5 | Payee Address | site address lines + `city, province, postal` flattened |
| 6 | Invoice Date | Level 1 approval date |
| 7 | Invoice Number | `payment.InvoiceNumber` |
| 8 | Amount | `payment.Amount` |
| 9 | Pay Group | `EFT` / `Cheque` |
| 10 | Goods/Services Received Date | Level 1 approval date |
| 11 | Qualifier Receiver | Level 1 approver name |
| 12 | QR Approval Date | Level 1 approval date |
| 13 | Expense Authority | Level 2 approver name |
| 14 | EA Approval Date | Level 2 approval date |
| 15 | CAS Cheque Stub Description | `payment.Description` |
| 16 | Account Coding | `AccountCodingFormatter.Format(payment.AccountCoding)` |
| 17 | Payment Requester | `payment.CreatorId` resolved to a name |
| 18 | Requested On | `payment.CreationTime` |
| 19 | L3 Approver | Level 3 approver name |
| 20 | L3 Approval Date | Level 3 approval date |

Columns 6, 10 and 12 all carry the same Level 1 approval date by design. Approver and requester names come from a single batched `IIdentityUserRepository.GetListByIdsAsync` over every distinct decision-user and creator id (`BuildUserNameDictionaryAsync`), so the spreadsheet costs one identity query per batch, not one per row. Missing site data falls back to `"N/A"` (and `"N/A/N/A"` for the supplier/site pair). A row that throws while being built is logged and skipped; if **every** row fails, the email for that batch is not sent.

Dates are written in Pacific time (`FsbPaymentExcelGenerator` resolves `Pacific Standard Time` / `America/Vancouver` / `America/Los_Angeles`, and falls back to UTC when none is available on the host).

### The round trip: stamping `FsbApNotified`

The payment is only marked as notified once CHES actually accepted the message:

```text
FsbPaymentNotifier → EmailNotificationEvent (Action = SendFsbNotification)
        ↓
Notifications EmailNotificationHandler.HandleFsbNotification
        creates the EmailLog, uploads the attachment to S3,
        stores the payment ids as a comma-separated EmailLog.PaymentRequestIds,
        marks the recipient Internal
        ↓
RabbitMQ → EmailConsumer → CHES
        ↓  (only on a success status code)
EmailConsumer publishes FsbEmailSentEto { EmailLogId, PaymentRequestIds, SentDate, TenantId }
        ↓
Payments FsbEmailSentEventHandler
        for each payment: SetFsbNotificationEmailLog(emailLogId, sentDate)
        → FsbNotificationEmailLogId, FsbNotificationSentDate, FsbApNotified = "Yes"
```

`FsbEmailSentEventHandler` (`Handlers/FsbEmailSentEventHandler.cs`) switches into the event's tenant, opens a new transactional unit of work, and updates each payment; a failure on one payment is logged and the rest continue, but a failure of the batch as a whole is rethrown as `InvalidOperationException`.

`FsbApNotified` is a Payments-list column, so staff can see at a glance which FSB payments have actually been sent to AP. `ClearFsbNotificationEmailLog()` exists on the entity to reset all three fields.

## The nightly failed-payment summary

`FinancialNotificationSummaryWorker` is a `[DisallowConcurrentExecution]` `QuartzBackgroundWorkerBase`. Its cron comes from `PaymentSettingsConstants.BackgroundJobs.CasFinancialNotificationSummary_ProducerExpression` (default `0 0 9 1/1 * ? *` — 09:00 UTC, 01:00 Pacific), with `0 0 9 1/1 * ? *` as the hard-coded fallback if the setting cannot be read.

It injects `IEnumerable<IEmailRecipientStrategy>` and passes it to `FinancialSummaryNotifier.NotifyFailedPayments`, which for **every tenant**:

1. Changes into the tenant context.
2. Calls `IPaymentRequestRepository.GetPaymentRequestsByFailedsStatusAsync()` — payments whose CAS `InvoiceStatus` is `ServiceUnavailable` or `Error` **and** whose `LastModificationTime` is within the last 24 hours (UTC). The 24-hour window is what stops the same failure being reported forever.
3. Builds an HTML table with `Unity.Payments.Shared/HtmlTable.cs` (`Table` / `Row` disposable writers) headed "Failed CAS Payment Requests", with columns **Payment Id** (`ReferenceNumber`), **Amount**, **Applicant Name** (`PayeeName`), **CAS Response**.
4. Unions the recipients from every registered strategy into a case-insensitive `HashSet`. A strategy that throws is logged as a warning and skipped; **no recipients at all** skips the tenant.
5. Publishes `EmailNotificationEvent { Action = SendFailedSummary, TenantId, ApplicationId = Guid.Empty, Body = table, EmailFrom = default-from-address }`.

Nothing is sent when a tenant has no recent failures.

## Payment status change events

`PaymentStatusChangedEvent` (`Events/PaymentStatusChangedEvent.cs`) carries `PaymentRequestId`, `ApplicationId` (the correlation id), `Status` and `TenantId`. It is published from `PaymentRequestAppService`:

- once per payment in `CreateAsync` and `CreateHistoricalAsync`,
- in `UpdateStatusAsync`, only when the status actually changed,
- in `CancelAsync`.

The host's `Events/ScheduledNotificationEventHandler` subscribes to it and, when a `ScheduledNotification` is configured for that form and payment status, publishes a templated `EmailNotificationEvent` to the applicant. Payments neither knows nor cares whether such a configuration exists.
