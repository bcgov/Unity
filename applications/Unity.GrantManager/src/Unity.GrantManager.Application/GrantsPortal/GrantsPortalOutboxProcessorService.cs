using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Unity.GrantManager.GrantsPortal.Configuration;
using Unity.GrantManager.Messaging;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal;

/// <summary>
/// Polls the central outbox table for pending acknowledgment messages and publishes them to RabbitMQ.
/// Uses publisher confirms to ensure delivery before marking messages as sent.
/// No tenant context needed — the outbox table is in the host database.
/// </summary>
public class GrantsPortalOutboxProcessorService(
    IServiceProvider serviceProvider,
    IAsyncConnectionFactory connectionFactory,
    IOptions<GrantsPortalRabbitMqOptions> options,
    ILogger<GrantsPortalOutboxProcessorService> logger) : BackgroundService
{    
    private IConnection? _connection;
    private IModel? _channel;

    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan IdleInterval = TimeSpan.FromSeconds(15);
    private const int MaxPublishRetries = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Grants Portal outbox processor starting...");

        // Wait for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                EnsureChannel();
                var processedAny = await PublishPendingAcksAsync(stoppingToken);
                var delay = processedAny ? PollingInterval : IdleInterval;
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in outbox processor loop. Will retry after delay.");
                CleanupChannel();
                await Task.Delay(IdleInterval, stoppingToken);
            }
        }

        logger.LogInformation("Grants Portal outbox processor stopped.");
    }

    private void EnsureChannel()
    {
        if (_channel is { IsOpen: true }) return;

        CleanupChannel();

        _connection = connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ConfirmSelect();

        logger.LogInformation("Outbox processor RabbitMQ channel established");
    }

    private async Task<bool> PublishPendingAcksAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
        var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        List<OutboxMessage> pendingMessages;
        using (var uow = unitOfWorkManager.Begin(requiresNew: true))
        {
            pendingMessages = await outboxRepo.GetPendingAsync(GrantsPortalRabbitMqOptions.SourceName, 10);
            await uow.CompleteAsync(cancellationToken);
        }

        if (pendingMessages.Count == 0) return false;

        foreach (var outboxMsg in pendingMessages)
        {
            if (cancellationToken.IsCancellationRequested) break;

            await PublishSingleAckAsync(outboxMsg, outboxRepo, unitOfWorkManager);
        }

        return true;
    }

    private async Task PublishSingleAckAsync(
        OutboxMessage outboxMsg,
        IOutboxMessageRepository outboxRepo,
        IUnitOfWorkManager unitOfWorkManager)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<GrantsPortalAcknowledgmentPublisher>();

            publisher.Publish(
                _channel!,
                outboxMsg.OriginalMessageId,
                outboxMsg.CorrelationId,
                outboxMsg.AckStatus,
                outboxMsg.Details);

            // Wait for broker to confirm
            if (!_channel!.WaitForConfirms(TimeSpan.FromSeconds(5)))
            {
                throw new InvalidOperationException("Broker did not confirm ack publish");
            }

            // Mark as sent
            using var uow = unitOfWorkManager.Begin(requiresNew: true);
            outboxMsg.Status = MessageStatus.Processed;
            outboxMsg.PublishedAt = DateTime.UtcNow;
            await outboxRepo.UpdateAsync(outboxMsg, autoSave: true);
            await uow.CompleteAsync();

            logger.LogInformation("Outbox message {MessageId} published (ack for {OriginalMessageId})",
                outboxMsg.MessageId, outboxMsg.OriginalMessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish outbox message {MessageId}", outboxMsg.MessageId);

            outboxMsg.RetryCount++;
            if (outboxMsg.RetryCount >= MaxPublishRetries)
            {
                outboxMsg.Status = MessageStatus.Failed;
                outboxMsg.Details = $"Failed to publish after {MaxPublishRetries} attempts: {ex.Message}";
                logger.LogError("Outbox message {MessageId} marked as failed after {MaxRetries} publish attempts",
                    outboxMsg.MessageId, MaxPublishRetries);
            }

            using var uow = unitOfWorkManager.Begin(requiresNew: true);
            await outboxRepo.UpdateAsync(outboxMsg, autoSave: true);
            await uow.CompleteAsync();
        }
    }

    private void CleanupChannel()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error during outbox channel cleanup");
        }

        _channel = null;
        _connection = null;
    }

    public override void Dispose()
    {
        CleanupChannel();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
