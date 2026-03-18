using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Messaging;

/// <summary>
/// Base class for outbox processing workers. Provides the full publish loop:
/// poll pending → publish → mark sent/failed.
///
/// Subclasses provide the source name, Quartz schedule, and the actual publish implementation
/// via <see cref="PublishMessageAsync"/>.
/// </summary>
[DisallowConcurrentExecution]
public abstract class OutboxWorkerBase : QuartzBackgroundWorkerBase
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// The integration source discriminator (e.g. "GrantsPortal").
    /// Used to filter pending outbox messages.
    /// </summary>
    protected abstract string SourceName { get; }

    /// <summary>
    /// Maximum number of publish retry attempts before marking as failed.
    /// Override to customize per integration. Default is 3.
    /// </summary>
    protected virtual int MaxPublishRetries => 3;

    /// <summary>
    /// Maximum number of pending messages to fetch per polling cycle.
    /// Override to customize per integration. Default is 10.
    /// </summary>
    protected virtual int BatchSize => 10;

    protected OutboxWorkerBase(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override async Task Execute(IJobExecutionContext context)
    {        
        try
        {
            OnBeforePublishCycle();
            await PublishPendingMessagesAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in {WorkerName} execution. Resources will be reset on next run.", GetType().Name);
            OnPublishCycleError(ex);
        }
    }

    /// <summary>
    /// Called before each publish cycle. Use to ensure transport connections/channels are ready.
    /// </summary>
    protected virtual void OnBeforePublishCycle() { }

    /// <summary>
    /// Called when the publish cycle throws an unhandled exception. Use to clean up transport resources.
    /// </summary>
    protected virtual void OnPublishCycleError(Exception ex) { }

    /// <summary>
    /// Publishes a single outbox message to the external system.
    /// Implementations should throw on failure — the base class handles retry and status updates.
    /// </summary>
    /// <param name="scope">The current DI scope for resolving transport-specific services.</param>
    /// <param name="outboxMsg">The outbox message to publish.</param>
    protected abstract Task PublishMessageAsync(IServiceScope scope, OutboxMessage outboxMsg);

    private async Task PublishPendingMessagesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
        var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        List<OutboxMessage> pendingMessages;
        using (var uow = unitOfWorkManager.Begin(requiresNew: true))
        {
            pendingMessages = await outboxRepo.GetPendingAsync(SourceName, BatchSize);
            await uow.CompleteAsync();
        }

        if (pendingMessages.Count == 0) return;

        foreach (var outboxMsg in pendingMessages)
        {
            await PublishSingleAsync(outboxMsg, scope, outboxRepo, unitOfWorkManager);
        }
    }

    private async Task PublishSingleAsync(
        OutboxMessage outboxMsg,
        IServiceScope scope,
        IOutboxMessageRepository outboxRepo,
        IUnitOfWorkManager unitOfWorkManager)
    {
        try
        {
            await PublishMessageAsync(scope, outboxMsg);

            // Mark as sent
            using var uow = unitOfWorkManager.Begin(requiresNew: true);
            outboxMsg.Status = MessageStatus.Processed;
            outboxMsg.PublishedAt = DateTime.UtcNow;
            await outboxRepo.UpdateAsync(outboxMsg, autoSave: true);
            await uow.CompleteAsync();

            Logger.LogInformation("Outbox message {MessageId} published (ack for {OriginalMessageId})",
                outboxMsg.MessageId, outboxMsg.OriginalMessageId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to publish outbox message {MessageId}", outboxMsg.MessageId);

            outboxMsg.RetryCount++;
            if (outboxMsg.RetryCount >= MaxPublishRetries)
            {
                outboxMsg.Status = MessageStatus.Failed;
                outboxMsg.Details = $"Failed to publish after {MaxPublishRetries} attempts: {ex.Message}";
                Logger.LogError("Outbox message {MessageId} marked as failed after {MaxRetries} publish attempts",
                    outboxMsg.MessageId, MaxPublishRetries);
            }

            using var uow = unitOfWorkManager.Begin(requiresNew: true);
            await outboxRepo.UpdateAsync(outboxMsg, autoSave: true);
            await uow.CompleteAsync();
        }
    }
}
