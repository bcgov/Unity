using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Unity.GrantManager.GrantsPortal.Configuration;
using Unity.GrantManager.GrantsPortal.Handlers;
using Unity.GrantManager.GrantsPortal.Messages;
using Unity.GrantManager.Messaging;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal;

/// <summary>
/// Polls the central inbox table for pending inbound messages and processes them sequentially.
/// Switches to the correct tenant context only when executing the handler (domain operations).
/// On completion (success or failure), writes an outbound ack message to the same central table.
/// </summary>
public class GrantsPortalInboxProcessorService(
    IServiceProvider serviceProvider,
    ILogger<GrantsPortalInboxProcessorService> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan IdleInterval = TimeSpan.FromSeconds(15);
    private const int MaxRetryCount = 3;

    private static readonly Dictionary<string, string> s_userFriendlyErrors = new(StringComparer.OrdinalIgnoreCase)
    {
        { "EntityNotFoundException", "The requested record was not found. It may have been deleted." },
        { "DbUpdateConcurrencyException", "The record was modified by another process. Please try again." },
        { "AbpDbConcurrencyException", "The record was modified by another process. Please try again." }
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Grants Portal inbox processor starting...");

        // Wait for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processedAny = await ProcessPendingMessagesAsync(stoppingToken);
                var delay = processedAny ? PollingInterval : IdleInterval;
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in inbox processor loop");
                await Task.Delay(IdleInterval, stoppingToken);
            }
        }

        logger.LogInformation("Grants Portal inbox processor stopped.");
    }

    private async Task<bool> ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var inboxRepo = scope.ServiceProvider.GetRequiredService<IInboxMessageRepository>();
        var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        List<InboxMessage> pendingMessages;
        using (var uow = unitOfWorkManager.Begin(requiresNew: true))
        {
            pendingMessages = await inboxRepo.GetPendingAsync(GrantsPortalRabbitMqOptions.SourceName, 10);
            await uow.CompleteAsync();
        }

        if (pendingMessages.Count == 0) return false;

        foreach (var inboxMsg in pendingMessages)
        {
            if (cancellationToken.IsCancellationRequested) break;

            await ProcessSingleMessageAsync(scope, inboxMsg);
        }

        return true;
    }

    private async Task ProcessSingleMessageAsync(IServiceScope scope, InboxMessage inboxMsg)
    {
        var inboxRepo = scope.ServiceProvider.GetRequiredService<IInboxMessageRepository>();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
        var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
        var handlers = scope.ServiceProvider.GetServices<IPortalCommandHandler>();

        logger.LogInformation("Processing inbox message {MessageId} (dataType={DataType}, tenantId={TenantId})",
            inboxMsg.MessageId, inboxMsg.DataType, inboxMsg.TenantId);

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

            // Deserialize the payload
            var envelope = JsonConvert.DeserializeObject<PluginDataEnvelope>(inboxMsg.Payload)
                           ?? throw new JsonException("Failed to deserialize message payload");

            var payload = envelope.Data?.ToObject<PluginDataPayload>()
                          ?? throw new ArgumentException("Message data payload is missing");

            var handler = handlers.FirstOrDefault(h =>
                string.Equals(h.DataType, inboxMsg.DataType, StringComparison.OrdinalIgnoreCase));

            if (handler == null)
            {
                ackStatus = "FAILED";
                details = $"Unknown command type: {inboxMsg.DataType}";
                logger.LogWarning("No handler registered for dataType {DataType}", inboxMsg.DataType);
            }
            else
            {
                // Switch to tenant context ONLY for the domain handler execution
                using (currentTenant.Change(inboxMsg.TenantId))
                {
                    using var uow = unitOfWorkManager.Begin(requiresNew: true);
                    details = await handler.HandleAsync(payload);
                    await uow.CompleteAsync();
                }
                ackStatus = "SUCCESS";
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing inbox message {MessageId}", inboxMsg.MessageId);
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
                logger.LogInformation("Message {MessageId} will be retried (attempt {Attempt}/{MaxRetries})",
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
                Source = GrantsPortalRabbitMqOptions.SourceName,
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

        logger.LogInformation("Inbox message {MessageId} processed with status {Status}",
            inboxMsg.MessageId, ackStatus);
    }

    private static string ToUserFriendlyMessage(Exception ex)
    {
        var exType = ex.GetType().Name;

        if (s_userFriendlyErrors.TryGetValue(exType, out var friendly))
            return friendly;

        // Check inner exception type
        if (ex.InnerException != null)
        {
            var innerType = ex.InnerException.GetType().Name;
            if (s_userFriendlyErrors.TryGetValue(innerType, out var innerFriendly))
                return innerFriendly;
        }

        // For unrecognized exceptions, return a generic message — never leak stack traces
        return "An unexpected error occurred while processing your request. Please try again or contact support.";
    }

    private static bool IsTransientError(Exception ex)
    {
        var typeName = ex.GetType().Name;
        return typeName.Contains("Timeout", StringComparison.OrdinalIgnoreCase)
               || typeName.Contains("Concurrency", StringComparison.OrdinalIgnoreCase)
               || typeName.Contains("Transient", StringComparison.OrdinalIgnoreCase)
               || ex.InnerException is TimeoutException;
    }
}
