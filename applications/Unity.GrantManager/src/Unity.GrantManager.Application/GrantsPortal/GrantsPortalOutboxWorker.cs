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
    private readonly IAsyncConnectionFactory _connectionFactory;
    private IConnection? _connection;
    private IModel? _channel;

    protected override string SourceName => GrantsPortalRabbitMqOptions.SourceName;

    public GrantsPortalOutboxWorker(
        IServiceProvider serviceProvider,
        IAsyncConnectionFactory connectionFactory,
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

    protected override void OnBeforePublishCycle()
    {
        EnsureChannel();
    }

    protected override void OnPublishCycleError(Exception ex)
    {
        CleanupChannel();
    }

    protected override async Task PublishMessageAsync(IServiceScope scope, OutboxMessage outboxMsg)
    {
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

        await Task.CompletedTask;
    }

    private void EnsureChannel()
    {
        if (_channel is { IsOpen: true }) return;

        CleanupChannel();

        _connection = _connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ConfirmSelect();

        Logger.LogInformation("Outbox worker RabbitMQ channel established");
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
            Logger.LogDebug(ex, "Error during outbox channel cleanup");
        }

        _channel = null;
        _connection = null;
    }
}
