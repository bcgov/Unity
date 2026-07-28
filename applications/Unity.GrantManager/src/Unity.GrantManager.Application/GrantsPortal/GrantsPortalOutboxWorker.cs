using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using RabbitMQ.Client;
using Unity.GrantManager.GrantsPortal.Configuration;
using Unity.GrantManager.Messaging;

namespace Unity.GrantManager.GrantsPortal;

/// <summary>
/// Polls the central outbox table for pending GrantsPortal acknowledgment messages and publishes them to RabbitMQ.
/// Uses publisher confirms to ensure delivery before the base class marks messages as sent.
/// All orchestration logic (retry, status updates) is handled by <see cref="OutboxWorkerBase"/>.
/// </summary>
public class GrantsPortalOutboxWorker : OutboxWorkerBase
{
    private readonly IConnectionFactory _connectionFactory;
    private IConnection? _connection;
    private IChannel? _channel;

    protected override string SourceName => GrantsPortalRabbitMqOptions.SourceName;

    public GrantsPortalOutboxWorker(
        IServiceProvider serviceProvider,
        IConnectionFactory connectionFactory,
        IOptions<GrantsPortalRabbitMqOptions> options)
        : base(serviceProvider)
    {
        _connectionFactory = connectionFactory;

        var cronExpression = options.Value.OutboxProcessorCron;

        JobDetail = JobBuilder
            .Create<GrantsPortalOutboxWorker>()
            .WithIdentity(nameof(GrantsPortalOutboxWorker))
            .Build();

        Trigger = TriggerBuilder
            .Create()
            .WithIdentity(nameof(GrantsPortalOutboxWorker))
            .WithSchedule(CronScheduleBuilder.CronSchedule(cronExpression)
            .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
    }

    protected override void OnPublishCycleError(Exception ex)
    {
        CleanupChannel();
    }

    protected override async Task PublishMessageAsync(IServiceScope scope, OutboxMessage outboxMsg)
    {
        await EnsureChannelAsync();

        var publisher = scope.ServiceProvider.GetRequiredService<GrantsPortalAcknowledgmentPublisher>();

        // Publisher confirmations are enabled on the channel, so PublishAsync awaits the
        // broker confirmation and throws if the ack message is not confirmed.
        await publisher.PublishAsync(
            _channel!,
            outboxMsg.OriginalMessageId,
            outboxMsg.CorrelationId,
            outboxMsg.AckStatus,
            outboxMsg.Details);
    }

    private async Task EnsureChannelAsync()
    {
        if (_channel is { IsOpen: true }) return;

        CleanupChannel();

        _connection = await _connectionFactory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true));

        Logger.LogInformation("Outbox worker RabbitMQ channel established");
    }

    private void CleanupChannel()
    {
        try
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error during outbox channel cleanup");
        }

        _channel = null;
        _connection = null;
    }
}
