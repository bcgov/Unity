using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Messaging;

/// <summary>
/// Base class for inbox processing workers. Provides the full orchestration loop:
/// poll pending → mark processing → dispatch to handler → retry on transient errors → mark complete → write outbox ack.
///
/// Subclasses only need to provide the source name and configure the Quartz schedule in their constructor.
/// Handlers are resolved from DI as <see cref="IInboxMessageHandler"/> filtered by <see cref="SourceName"/>.
/// </summary>
[DisallowConcurrentExecution]
public abstract class InboxWorkerBase : QuartzBackgroundWorkerBase
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// The integration source discriminator (e.g. "GrantsPortal").
    /// Used to filter pending inbox messages and tag outbox acknowledgments.
    /// </summary>
    protected abstract string SourceName { get; }

    /// <summary>
    /// Maximum number of retry attempts for transient errors before marking as failed.
    /// Override to customize per integration. Default is 3.
    /// </summary>
    protected virtual int MaxRetryCount => 3;

    /// <summary>
    /// Maximum number of pending messages to fetch per polling cycle.
    /// Override to customize per integration. Default is 10.
    /// </summary>
    protected virtual int BatchSize => 10;

    private static readonly Dictionary<string, string> s_userFriendlyErrors = new(StringComparer.OrdinalIgnoreCase)
    {
        { "EntityNotFoundException", "The requested record was not found. It may have been deleted." },
        { "DbUpdateConcurrencyException", "The record was modified by another process. Please try again." },
        { "AbpDbConcurrencyException", "The record was modified by another process. Please try again." }
    };

    /// <summary>
    /// Exception types whose <see cref="Exception.Message"/> is safe to surface verbatim
    /// in outbox acknowledgments. These are input/validation errors thrown by handlers
    /// (e.g., missing required fields, malformed GUIDs, deserialization failures).
    /// </summary>
    private static readonly HashSet<string> s_validationExceptionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ArgumentException",
        "ArgumentNullException",
        "FormatException",
        "JsonException",
        "JsonReaderException",
        "JsonSerializationException"
    };

    protected InboxWorkerBase(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override async Task Execute(IJobExecutionContext context)
    {        
        try
        {
            await ProcessPendingMessagesAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error in {WorkerName} execution", GetType().Name);
        }
    }

    private async Task ProcessPendingMessagesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var inboxRepo = scope.ServiceProvider.GetRequiredService<IInboxMessageRepository>();
        var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        List<InboxMessage> pendingMessages;
        using (var uow = unitOfWorkManager.Begin(requiresNew: true))
        {
            pendingMessages = await inboxRepo.GetPendingAsync(SourceName, BatchSize);
            await uow.CompleteAsync();
        }

        if (pendingMessages.Count == 0) return;

        foreach (var inboxMsg in pendingMessages)
        {
            await ProcessSingleMessageAsync(scope, inboxMsg);
        }
    }

    private async Task ProcessSingleMessageAsync(IServiceScope scope, InboxMessage inboxMsg)
    {
        var inboxRepo = scope.ServiceProvider.GetRequiredService<IInboxMessageRepository>();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
        var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
        var handlers = scope.ServiceProvider.GetServices<IInboxMessageHandler>();

        Logger.LogInformation("Processing inbox message {MessageId} (source={Source}, dataType={DataType}, tenantId={TenantId})",
            inboxMsg.MessageId, inboxMsg.Source, inboxMsg.DataType, inboxMsg.TenantId);

        string ackStatus;
        string details;

        try
        {
            // Mark as processing
            using (var uow = unitOfWorkManager.Begin(requiresNew: true))
            {
                inboxMsg.Status = MessageStatus.Processing;
                inboxMsg.RetryCount++;
                await inboxRepo.UpdateAsync(inboxMsg, autoSave: true);
                await uow.CompleteAsync();
            }

            var handler = handlers.FirstOrDefault(h =>
                string.Equals(h.Source, SourceName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(h.DataType, inboxMsg.DataType, StringComparison.OrdinalIgnoreCase));

            if (handler == null)
            {
                ackStatus = "FAILED";
                details = $"Unknown command type: {inboxMsg.DataType}";
                Logger.LogWarning("No handler registered for source {Source}, dataType {DataType}",
                    SourceName, inboxMsg.DataType);
            }
            else
            {
                // Switch to tenant context ONLY for the domain handler execution
                using (currentTenant.Change(inboxMsg.TenantId))
                {
                    using var uow = unitOfWorkManager.Begin(requiresNew: true);
                    details = await handler.HandleAsync(inboxMsg.Payload);
                    await uow.CompleteAsync();
                }
                ackStatus = "SUCCESS";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing inbox message {MessageId}", inboxMsg.MessageId);
            ackStatus = "FAILED";
            details = ToUserFriendlyMessage(ex);

            // Check if we should retry
            if (inboxMsg.RetryCount < MaxRetryCount && IsTransientError(ex))
            {
                using var uow = unitOfWorkManager.Begin(requiresNew: true);
                inboxMsg.Status = MessageStatus.Pending;
                inboxMsg.Details = details;
                await inboxRepo.UpdateAsync(inboxMsg, autoSave: true);
                await uow.CompleteAsync();
                Logger.LogInformation("Message {MessageId} will be retried (attempt {Attempt}/{MaxRetries})",
                    inboxMsg.MessageId, inboxMsg.RetryCount, MaxRetryCount);
                return;
            }
        }

        // Mark inbox as complete + write to outbox — same transaction
        using (var uow = unitOfWorkManager.Begin(requiresNew: true))
        {
            inboxMsg.Status = ackStatus == "SUCCESS" ? MessageStatus.Processed : MessageStatus.Failed;
            inboxMsg.Details = details;
            inboxMsg.ProcessedAt = DateTime.UtcNow;
            await inboxRepo.UpdateAsync(inboxMsg, autoSave: true);

            var outboxMsg = new OutboxMessage
            {
                Source = SourceName,
                MessageId = Guid.NewGuid().ToString(),
                OriginalMessageId = inboxMsg.MessageId,
                CorrelationId = inboxMsg.CorrelationId,
                DataType = inboxMsg.DataType,
                AckStatus = ackStatus,
                Details = details,
                Status = MessageStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                TenantId = inboxMsg.TenantId
            };

            await outboxRepo.InsertAsync(outboxMsg, autoSave: true);
            await uow.CompleteAsync();
        }

        Logger.LogInformation("Inbox message {MessageId} processed with status {Status}",
            inboxMsg.MessageId, ackStatus);
    }

    /// <summary>
    /// Maps exception types to user-friendly messages. Override to add integration-specific mappings.
    /// </summary>
    protected virtual string ToUserFriendlyMessage(Exception ex)
    {
        var exType = ex.GetType().Name;

        if (s_userFriendlyErrors.TryGetValue(exType, out var friendly))
            return friendly;

        if (ex.InnerException != null)
        {
            var innerType = ex.InnerException.GetType().Name;
            if (s_userFriendlyErrors.TryGetValue(innerType, out var innerFriendly))
                return innerFriendly;
        }

        // Validation / input errors — surface ex.Message so callers get actionable feedback
        if (IsValidationException(ex))
            return ex.Message;

        return "An unexpected error occurred while processing your request. Please try again or contact support.";
    }

    /// <summary>
    /// Returns true when the exception (or its inner exception) is a validation/input error
    /// whose message is safe to include in outbox acknowledgments.
    /// </summary>
    private static bool IsValidationException(Exception ex)
    {
        if (s_validationExceptionTypes.Contains(ex.GetType().Name))
            return true;

        return ex.InnerException != null
            && s_validationExceptionTypes.Contains(ex.InnerException.GetType().Name);
    }

    /// <summary>
    /// Determines if an error is transient (eligible for retry). Override to add integration-specific checks.
    /// </summary>
    protected virtual bool IsTransientError(Exception ex)
    {
        var typeName = ex.GetType().Name;
        return typeName.Contains("Timeout", StringComparison.OrdinalIgnoreCase)
               || typeName.Contains("Concurrency", StringComparison.OrdinalIgnoreCase)
               || typeName.Contains("Transient", StringComparison.OrdinalIgnoreCase)
               || ex.InnerException is TimeoutException;
    }
}
