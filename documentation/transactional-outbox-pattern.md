# Transactional Outbox Pattern

## Overview

Unity Grant Manager uses the **Transactional Inbox/Outbox** pattern for reliable asynchronous messaging with external systems. The pattern ensures that message receipt, processing, and response publishing are each atomic operations — even if the broker or application crashes mid-flow.

The implementation is **integration-source agnostic**. The same `InboxMessage` and `OutboxMessage` tables, entities, and repositories are shared by all integrations. Each integration is identified by a `Source` discriminator (e.g. `"GrantsPortal"`).

---

## Why This Pattern

Direct RabbitMQ consumption with inline processing has several failure modes:

| Problem | Without Outbox | With Outbox |
|---------|---------------|-------------|
| App crashes after processing but before ACK | Message redelivered, duplicate side-effects | Message already saved to inbox; ACK happened at save time |
| Broker unavailable when sending response | Response lost | Response saved to outbox table; publisher retries independently |
| Database commit fails after ACK | ACK'd but no state change | ACK only happens after inbox save commits |
| Need to audit message history | Logs only | Full database trail with status, timestamps, retry counts |

---

## Architecture

The pattern separates the messaging pipeline into four independent stages:

```mermaid
graph LR
    RMQ[(RabbitMQ<br/>Broker)]

    subgraph Unity["Unity Grant Manager — Host Database"]
        S1["① Consumer<br/>BackgroundService"]
        IT[(InboxMessages<br/>Table)]
        S2["② Inbox Worker<br/>Quartz [DisallowConcurrentExecution]"]
        OT[(OutboxMessages<br/>Table)]
        S3["③ Outbox Worker<br/>Quartz [DisallowConcurrentExecution]"]
        S4["④ Cleanup Worker<br/>Quartz [DisallowConcurrentExecution]"]
    end

    RMQ -->|"Consume + ACK"| S1
    S1 -->|"INSERT (Pending)"| IT
    S2 -->|"Poll Pending"| IT
    S2 -->|"Process → INSERT ack"| OT
    S3 -->|"Poll Pending"| OT
    S3 -->|"Publish + Confirm"| RMQ
    S4 -.->|"DELETE old rows"| IT
    S4 -.->|"DELETE old rows"| OT

    style IT fill:#e8f5e9
    style OT fill:#fff4e6
```

### Stage Responsibilities

| # | Stage | Scope | Transaction Boundary |
|---|-------|-------|---------------------|
| ① | **Consumer** | Receive from broker → save to inbox → ACK | Inbox INSERT committed before ACK sent |
| ② | **Inbox Processor** | Poll inbox → dispatch to handler → write ack to outbox | Handler execution + outbox INSERT in one UoW |
| ③ | **Outbox Processor** | Poll outbox → publish to broker → mark as sent | Publish with broker confirms before UPDATE |
| ④ | **Cleanup** | Delete old Processed/Failed rows | Periodic bulk delete |

---

## Domain Entities

Both entities live in `Unity.GrantManager.Domain/Messaging/` and are stored in the **host database** (`GrantManagerDbContext`), not in tenant databases. They inherit from ABP's `AuditedAggregateRoot<Guid>`.

### InboxMessage

Represents a message received from an external system, staged for sequential processing.

```
Unity.GrantManager.Domain/Messaging/InboxMessage.cs
```

| Property | Type | Description |
|----------|------|-------------|
| `Source` | `string` | Integration discriminator (e.g. `"GrantsPortal"`) |
| `MessageId` | `string` | Source system's message ID — used for **idempotency** |
| `CorrelationId` | `string` | Correlation ID passed through from the source |
| `DataType` | `string` | Command discriminator (e.g. `CONTACT_CREATE_COMMAND`) |
| `Payload` | `string` | Full JSON payload of the inbound message |
| `Status` | `MessageStatus` | `Pending` → `Processing` → `Processed` / `Failed` |
| `Details` | `string?` | Processing result or user-friendly error message |
| `RetryCount` | `int` | Number of processing attempts |
| `ReceivedAt` | `DateTime` | When the message arrived from the broker |
| `ProcessedAt` | `DateTime?` | When processing completed |
| `TenantId` | `Guid?` | Tenant context for handler dispatch (metadata, not data isolation) |

### OutboxMessage

Represents a response/acknowledgment to be published back to an external system.

```
Unity.GrantManager.Domain/Messaging/OutboxMessage.cs
```

| Property | Type | Description |
|----------|------|-------------|
| `Source` | `string` | Integration discriminator |
| `MessageId` | `string` | Unique ID for this outbound message |
| `OriginalMessageId` | `string` | The inbound message ID this responds to |
| `CorrelationId` | `string` | Correlation ID from the original message |
| `DataType` | `string` | Command type of the original message |
| `AckStatus` | `string` | `SUCCESS` or `FAILED` |
| `Details` | `string` | Human-readable result or error (safe for end-user display) |
| `Status` | `MessageStatus` | `Pending` → `Processed` / `Failed` |
| `RetryCount` | `int` | Number of publish attempts |
| `CreatedAt` | `DateTime` | When the outbox entry was created |
| `PublishedAt` | `DateTime?` | When the message was confirmed by the broker |
| `TenantId` | `Guid?` | Tenant context metadata |

### MessageStatus Enum

```csharp
public enum MessageStatus
{
    Pending = 1,
    Processing = 2,
    Processed = 3,
    Failed = 4
}
```

---

## Database Tables

Both tables are in the **host database** and were added in migration `20260307013604_Add_InboxOutboxMessages`.

### InboxMessages

```sql
CREATE TABLE "InboxMessages" (
    "Id"                   UUID PRIMARY KEY,
    "Source"               VARCHAR(50)   NOT NULL,
    "MessageId"            VARCHAR(64)   NOT NULL,
    "CorrelationId"        VARCHAR(128)  NOT NULL,
    "DataType"             VARCHAR(100)  NOT NULL,
    "Payload"              JSONB         NOT NULL,
    "Status"               TEXT          NOT NULL,
    "Details"              VARCHAR(2000),
    "RetryCount"           INTEGER       NOT NULL DEFAULT 0,
    "ReceivedAt"           TIMESTAMP     NOT NULL,
    "ProcessedAt"          TIMESTAMP,
    "TenantId"             UUID,
    -- ABP audit columns
    "ExtraProperties"      TEXT          NOT NULL,
    "ConcurrencyStamp"     VARCHAR(40)   NOT NULL,
    "CreationTime"         TIMESTAMP     NOT NULL,
    "CreatorId"            UUID,
    "LastModificationTime" TIMESTAMP,
    "LastModifierId"       UUID
);

CREATE UNIQUE INDEX "IX_InboxMessages_MessageId"
    ON "InboxMessages" ("MessageId");

CREATE INDEX "IX_InboxMessages_Source_Status"
    ON "InboxMessages" ("Source", "Status");
```

### OutboxMessages

```sql
CREATE TABLE "OutboxMessages" (
    "Id"                   UUID PRIMARY KEY,
    "Source"               VARCHAR(50)   NOT NULL,
    "MessageId"            VARCHAR(64)   NOT NULL,
    "OriginalMessageId"    VARCHAR(64)   NOT NULL,
    "CorrelationId"        VARCHAR(128)  NOT NULL,
    "DataType"             VARCHAR(100)  NOT NULL,
    "AckStatus"            VARCHAR(20)   NOT NULL,
    "Details"              VARCHAR(2000) NOT NULL,
    "Status"               TEXT          NOT NULL,
    "RetryCount"           INTEGER       NOT NULL DEFAULT 0,
    "CreatedAt"            TIMESTAMP     NOT NULL,
    "PublishedAt"          TIMESTAMP,
    "TenantId"             UUID,
    -- ABP audit columns
    "ExtraProperties"      TEXT          NOT NULL,
    "ConcurrencyStamp"     VARCHAR(40)   NOT NULL,
    "CreationTime"         TIMESTAMP     NOT NULL,
    "CreatorId"            UUID,
    "LastModificationTime" TIMESTAMP,
    "LastModifierId"       UUID
);

CREATE INDEX "IX_OutboxMessages_Source_Status"
    ON "OutboxMessages" ("Source", "Status");
```

---

## Repository Interfaces

Both repositories extend ABP's `IRepository<TEntity, Guid>` and add integration-specific queries.

```
Unity.GrantManager.Domain/Messaging/IInboxMessageRepository.cs
Unity.GrantManager.Domain/Messaging/IOutboxMessageRepository.cs
```

### IInboxMessageRepository

| Method | Description |
|--------|-------------|
| `FindByMessageIdAsync(string messageId)` | Idempotency check — find by source message ID |
| `GetPendingAsync(string source, int maxCount)` | Poll for messages with `Status == Pending`, ordered by `ReceivedAt` |
| `DeleteProcessedOlderThanAsync(DateTime cutoff)` | Bulk delete `Processed` or `Failed` rows older than cutoff |

### IOutboxMessageRepository

| Method | Description |
|--------|-------------|
| `GetPendingAsync(string source, int maxCount)` | Poll for messages with `Status == Pending`, ordered by `CreatedAt` |
| `DeleteProcessedOlderThanAsync(DateTime cutoff)` | Bulk delete `Processed` or `Failed` rows older than cutoff |

EF Core implementations are in `Unity.GrantManager.EntityFrameworkCore/Repositories/` and use `GrantManagerDbContext` (host DB).

---

## Message Lifecycle

```mermaid
stateDiagram-v2
    direction LR

    state "Inbox" as inbox {
        [*] --> Pending : Consumer saves
        Pending --> Processing : Processor picks up
        Processing --> Processed : Handler succeeds
        Processing --> Failed : Handler fails (max retries)
        Processing --> Pending : Transient error (retry)
    }

    state "Outbox" as outbox {
        [*] --> OPending : Processor writes ack
        OPending --> OProcessed : Publisher confirms
        OPending --> OFailed : Max publish retries

        state "Pending" as OPending
        state "Processed" as OProcessed
        state "Failed" as OFailed
    }

    inbox --> outbox : On inbox completion
```

### Detailed Flow

1. **Consumer receives** a message from the broker.
2. Consumer saves it to `InboxMessages` with `Status = Pending` inside a Unit of Work.
3. After the UoW commits, the consumer **ACKs** the broker delivery. If the save fails, the message is **rejected/requeued**.
4. **Inbox Processor** polls `InboxMessages` for `Pending` rows (filtered by `Source`).
5. For each message, the processor:
   - Sets `Status = Processing` and increments `RetryCount`
   - Deserializes the payload and dispatches to the appropriate handler
   - On **success**: sets `Status = Processed` and writes an `OutboxMessage` with `AckStatus = "SUCCESS"` — both in the **same Unit of Work**
   - On **transient failure** (under max retries): resets `Status = Pending` for retry
   - On **permanent failure** (or max retries exceeded): sets `Status = Failed` and writes an `OutboxMessage` with `AckStatus = "FAILED"`
6. **Outbox Processor** polls `OutboxMessages` for `Pending` rows.
7. For each message, the processor:
   - Publishes to the broker using **publisher confirms**
   - After broker confirmation: sets `Status = Processed` and records `PublishedAt`
   - On failure (under max retries): increments `RetryCount`
   - On max retries exceeded: sets `Status = Failed`
8. **Cleanup Service** periodically deletes `Processed` and `Failed` rows older than the retention period.

---

## Idempotency

The consumer performs an idempotency check before saving to the inbox:

```
FindByMessageIdAsync(messageId) → if exists, ACK and skip
```

This prevents duplicate inbox rows if the broker redelivers a message (e.g. after a network hiccup before the ACK reached the broker).

**Multi-pod safety**: In a multi-pod deployment, two pods could race past the `FindByMessageIdAsync` check on the same redelivered message. The `MessageId` column has a **unique index** (`IX_InboxMessages_MessageId`) as the definitive guard. If the second pod’s insert hits the unique constraint (PostgreSQL error `23505`), the consumer catches it and treats it as idempotent success — ACKs without requeueing.

---

## Error Handling

### User-Friendly Error Messages

The inbox processor maps known exception types to user-friendly messages that are safe to return to the external system:

| Exception Type | User-Facing Message |
|---------------|-------------------|
| `EntityNotFoundException` | The requested record was not found. It may have been deleted. |
| `DbUpdateConcurrencyException` | The record was modified by another process. Please try again. |
| `AbpDbConcurrencyException` | The record was modified by another process. Please try again. |
| _(any other)_ | An unexpected error occurred while processing your request. Please try again or contact support. |

Stack traces and internal details are **never** leaked to the external system.

### Transient Error Detection

Errors are considered transient (eligible for retry) if the exception type name contains `Timeout`, `Concurrency`, or `Transient`, or if the inner exception is a `TimeoutException`.

---

## Cleanup / Retention

A dedicated Quartz worker runs hourly and deletes `Processed` and `Failed` messages older than the configured retention period. The default retention is **30 days**.

Both inbox and outbox tables are cleaned in the same pass.

---

## Adding a New Integration Source

The inbox/outbox infrastructure provides base classes that handle all orchestration logic.
A new integration only needs to provide source-specific configuration, handlers, and a publish implementation.

### What you get for free

| Concern | Provided by |
|---------|------------|
| Poll pending → mark processing → dispatch → retry → mark complete → write outbox ack | `InboxWorkerBase` (`Unity.GrantManager.Application/Messaging/`) |
| Poll pending outbox → publish → mark sent/failed with retry | `OutboxWorkerBase` (`Unity.GrantManager.Application/Messaging/`) |
| Handler dispatch by `Source` + `DataType` | `IInboxMessageHandler` (`Unity.GrantManager.Domain/Messaging/`) |
| Shared tables, entities, repos, status machine | `InboxMessage`, `OutboxMessage`, `IInboxMessageRepository`, `IOutboxMessageRepository` |
| Message cleanup | Existing `GrantsPortalMessageCleanupWorker` (deletes all sources — can be reused or cloned) |

### Step-by-step

#### 1. Choose a source name

Pick a unique string (e.g. `"Finance"`) that will be stored in the `Source` column of both tables.

#### 2. Create an options class

```csharp
// YourIntegration/Configuration/FinanceIntegrationOptions.cs
public class FinanceIntegrationOptions
{
    public const string SectionName = "Integrations:Finance";
    public const string SourceName = "Finance";

    public string InboxProcessorCron { get; set; } = "0/10 * * * * ?";
    public string OutboxProcessorCron { get; set; } = "0/10 * * * * ?";

    // Add transport-specific properties (endpoints, queues, API keys, etc.)
}
```

#### 3. Create message handlers

Implement `IInboxMessageHandler` for each command type. Each handler receives the raw JSON payload and is responsible for its own deserialization:

```csharp
// YourIntegration/Handlers/InvoiceCreatedHandler.cs
public class InvoiceCreatedHandler : IInboxMessageHandler, ITransientDependency
{
    public string Source => FinanceIntegrationOptions.SourceName;
    public string DataType => "INVOICE_CREATED";

    public async Task<string> HandleAsync(string rawPayload)
    {
        var data = JsonConvert.DeserializeObject<InvoicePayload>(rawPayload)
                   ?? throw new JsonException("Invalid payload");

        // Domain logic here...

        return "Invoice processed successfully";
    }
}
```

#### 4. Create an inbox worker

Subclass `InboxWorkerBase` — typically ~15 lines:

```csharp
// YourIntegration/FinanceInboxWorker.cs
public class FinanceInboxWorker : InboxWorkerBase
{
    protected override string SourceName => FinanceIntegrationOptions.SourceName;

    public FinanceInboxWorker(
        IServiceProvider serviceProvider,
        IOptions<FinanceIntegrationOptions> options)
        : base(serviceProvider)
    {
        JobDetail = JobBuilder.Create<FinanceInboxWorker>()
            .WithIdentity(nameof(FinanceInboxWorker)).Build();

        Trigger = TriggerBuilder.Create()
            .WithIdentity(nameof(FinanceInboxWorker))
            .WithSchedule(CronScheduleBuilder.CronSchedule(options.Value.InboxProcessorCron)
                .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
    }
}
```

The base class handles: polling, status transitions, tenant context switching, handler dispatch by `Source` + `DataType`, transient error retry, user-friendly error mapping, and writing the outbox ack.

Override `ToUserFriendlyMessage()` or `IsTransientError()` if your integration has custom error types.

#### 5. Create an outbox worker

Subclass `OutboxWorkerBase` and implement `PublishMessageAsync`:

```csharp
// YourIntegration/FinanceOutboxWorker.cs
public class FinanceOutboxWorker : OutboxWorkerBase
{
    protected override string SourceName => FinanceIntegrationOptions.SourceName;

    public FinanceOutboxWorker(
        IServiceProvider serviceProvider,
        IOptions<FinanceIntegrationOptions> options)
        : base(serviceProvider)
    {
        JobDetail = JobBuilder.Create<FinanceOutboxWorker>()
            .WithIdentity(nameof(FinanceOutboxWorker)).Build();

        Trigger = TriggerBuilder.Create()
            .WithIdentity(nameof(FinanceOutboxWorker))
            .WithSchedule(CronScheduleBuilder.CronSchedule(options.Value.OutboxProcessorCron)
                .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
    }

    protected override async Task PublishMessageAsync(IServiceScope scope, OutboxMessage outboxMsg)
    {
        // Your transport-specific publish logic here (HTTP, RabbitMQ, gRPC, etc.)
        // Throw on failure — the base class handles retry and status updates.
    }

    // Optional: override OnBeforePublishCycle() to ensure connections are ready
    // Optional: override OnPublishCycleError() to clean up connections on failure
}
```

#### 6. Create a consumer (optional — depends on your transport)

If consuming from a message broker (RabbitMQ, Kafka, etc.), create a `BackgroundService` that:
- Receives messages from the broker
- Saves them to `InboxMessages` with your source name inside a Unit of Work
- ACKs the broker only after the UoW commits

See `GrantsPortalCommandConsumerService` for a RabbitMQ reference implementation.

If your inbound messages arrive via HTTP webhook, save them to the inbox table in the webhook endpoint controller instead.

#### 7. Register in DI

In your module's `ConfigureServices`:

```csharp
// Options
context.Services.Configure<FinanceIntegrationOptions>(
    configuration.GetSection(FinanceIntegrationOptions.SectionName));

// Handlers (auto-registered if using ITransientDependency, otherwise register explicitly)
context.Services.AddTransient<IInboxMessageHandler, InvoiceCreatedHandler>();

// Consumer (if using a BackgroundService)
context.Services.AddHostedService<FinanceConsumerService>();
```

The Quartz inbox/outbox workers auto-register when `BackgroundJobs:Quartz:IsAutoRegisterEnabled` is `true`.

#### 8. Cleanup

The existing `GrantsPortalMessageCleanupWorker` deletes all `Processed`/`Failed` rows regardless of source. If the default 30-day retention works for your integration, no additional cleanup worker is needed.

### What you do NOT need to do

- No schema changes — the shared tables already support multiple sources via the `Source` column
- No changes to existing integrations — each source's workers and handlers are fully independent
- No custom orchestration logic — `InboxWorkerBase` and `OutboxWorkerBase` handle the full lifecycle

### Future considerations

| Area | Status | Notes |
|------|--------|-------|
| Base options interface | Not yet extracted | Could extract `IIntegrationSourceOptions` with shared cron/retention fields |
| Base consumer service | Not yet extracted | RabbitMQ connection management could be shared; currently each consumer owns its own |
| Source-aware cleanup | Not yet needed | Current cleanup worker deletes all sources; could filter by source if retention policies differ |
