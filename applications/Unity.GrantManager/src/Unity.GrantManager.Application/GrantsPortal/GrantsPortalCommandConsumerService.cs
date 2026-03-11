using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Unity.GrantManager.GrantsPortal.Configuration;
using Unity.GrantManager.GrantsPortal.Messages;
using Unity.GrantManager.Messaging;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal;

/// <summary>
/// Pulls messages off the RabbitMQ queue, saves them to the inbox table, and ACKs immediately.
/// Runs on every pod as a competing consumer — RabbitMQ distributes messages round-robin.
/// Actual processing is done by <see cref="GrantsPortalInboxWorker"/>.
/// </summary>
public class GrantsPortalCommandConsumerService(
    IServiceProvider serviceProvider,
    IAsyncConnectionFactory connectionFactory,
    IOptions<GrantsPortalRabbitMqOptions> options,
    ILogger<GrantsPortalCommandConsumerService> logger) : BackgroundService
{
    private readonly GrantsPortalRabbitMqOptions _options = options.Value;

    // Guards against concurrent reconnect attempts within this process.
    // RabbitMQ can fire ConnectionShutdown multiple times in rapid succession
    // (e.g., network flap, broker restart) on different threadpool threads.
    // Without this, parallel Task.Run calls would race on the shared
    // _connection/_channel fields — one disposes while the other connects.
    // This is NOT for cross-pod coordination (RabbitMQ handles that).
    private readonly SemaphoreSlim _reconnectLock = new(1, 1);

    private IConnection? _connection;
    private IModel? _channel;

    private const int MaxRetries = 5;
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(5);
    private CancellationToken _stoppingToken;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;
        logger.LogInformation("Grants Portal command consumer starting...");

        await ConnectAndConsumeAsync(stoppingToken);

        // Keep the service alive until cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogInformation("Grants Portal command consumer stopping... {Ex}", ex.Message);
        }
    }

    private async Task ConnectAndConsumeAsync(CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                logger.LogInformation("Connecting to RabbitMQ for Grants Portal consumer (attempt {Attempt}/{MaxRetries})", attempt, MaxRetries);

                _connection = connectionFactory.CreateConnection();
                _connection.ConnectionShutdown += OnConnectionShutdown;
                _channel = _connection.CreateModel();
                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                DeclareTopology();
                StartConsuming();

                logger.LogInformation("Grants Portal command consumer started. Listening on queue {Queue}", _options.InboundQueue);
                return;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                var delay = TimeSpan.FromSeconds(InitialRetryDelay.TotalSeconds * Math.Pow(2, attempt - 1));
                logger.LogWarning(ex, "Failed to connect to RabbitMQ (attempt {Attempt}). Retrying in {Delay}s...", attempt, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to RabbitMQ after {MaxRetries} attempts : {Ex}", MaxRetries, ex);
                throw;
            }
        }
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        if (_stoppingToken.IsCancellationRequested) return;

        logger.LogWarning("RabbitMQ connection lost: {Reason}. Attempting to reconnect...", e.ReplyText);

        _ = Task.Run(async () =>
        {
            if (!await _reconnectLock.WaitAsync(0, _stoppingToken))
            {
                logger.LogDebug("Reconnect already in progress, skipping duplicate attempt");
                return;
            }

            try
            {
                await Task.Delay(InitialRetryDelay, _stoppingToken);
                CleanupConnection();
                await ConnectAndConsumeAsync(_stoppingToken);
            }
            catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested)
            {
                // Shutting down — expected
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to reconnect to RabbitMQ after connection loss");
            }
            finally
            {
                _reconnectLock.Release();
            }
        }, _stoppingToken);
    }

    private void DeclareTopology()
    {
        if (_channel == null) return;

        _channel.ExchangeDeclare(
            exchange: _options.Exchange,
            type: _options.ExchangeType,
            durable: true,
            autoDelete: false);

        _channel.QueueDeclare(
            queue: _options.InboundQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new System.Collections.Generic.Dictionary<string, object>
            {
                { "x-queue-type", "quorum" }
            });

        foreach (var routingKey in _options.InboundRoutingKeys)
        {
            _channel.QueueBind(
                queue: _options.InboundQueue,
                exchange: _options.Exchange,
                routingKey: routingKey);
        }

        logger.LogInformation(
            "Declared exchange {Exchange} (topic), queue {Queue}, bound with routing keys [{RoutingKeys}]",
            _options.Exchange, _options.InboundQueue, string.Join(", ", _options.InboundRoutingKeys));
    }

    private void StartConsuming()
    {
        if (_channel == null) return;

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceivedAsync;

        _channel.BasicConsume(
            queue: _options.InboundQueue,
            autoAck: false,
            consumer: consumer);
    }

    /// <summary>
    /// Receives a message from RabbitMQ and saves it to the inbox table.
    /// The message is ACKed immediately after saving — no processing happens here.
    /// </summary>
    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var messageId = ea.BasicProperties?.MessageId ?? string.Empty;
        var messageType = ea.BasicProperties?.Type ?? string.Empty;
        var correlationId = ea.BasicProperties?.CorrelationId ?? string.Empty;
        var consumingChannel = ((AsyncEventingBasicConsumer)sender).Model;

        logger.LogInformation("Received message {MessageId} type={MessageType}", messageId, messageType);

        // Guard: discard acknowledgment messages to prevent infinite loops (spec §4.2)
        if (string.Equals(messageType, "MessageAcknowledgment", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("Discarding acknowledgment message {MessageId} to prevent loop", messageId);
            consumingChannel.BasicAck(ea.DeliveryTag, multiple: false);
            return;
        }

        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var envelope = JsonConvert.DeserializeObject<PluginDataEnvelope>(json);

            if (envelope == null)
            {
                logger.LogError("Failed to deserialize message {MessageId}. Discarding.", messageId);
                consumingChannel.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            // Use envelope values as fallback for AMQP properties
            if (string.IsNullOrEmpty(messageId)) messageId = envelope.MessageId;
            if (string.IsNullOrEmpty(correlationId)) correlationId = envelope.CorrelationId;

            // Resolve tenant from the data.provider field (stored for later use by processors)
            var payload = envelope.Data?.ToObject<PluginDataPayload>();
            var tenantId = ResolveTenantId(payload?.Provider);

            // Save to the central host inbox — no tenant context needed
            using var scope = serviceProvider.CreateScope();
            var inboxRepo = scope.ServiceProvider.GetRequiredService<IInboxMessageRepository>();
            var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            using var uow = unitOfWorkManager.Begin(requiresNew: true);

            // Idempotency: skip if we already have this message
            var existing = await inboxRepo.FindByMessageIdAsync(messageId);
            if (existing != null)
            {
                logger.LogInformation("Message {MessageId} already in inbox (status={Status}). Skipping.", messageId, existing.Status);
                consumingChannel.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            var inboxMessage = new InboxMessage
            {
                Source = GrantsPortalRabbitMqOptions.SourceName,
                MessageId = messageId,
                CorrelationId = correlationId,
                DataType = envelope.DataType,
                Payload = json,
                Status = MessageStatus.Pending,
                ReceivedAt = DateTime.UtcNow,
                TenantId = tenantId
            };

            await inboxRepo.InsertAsync(inboxMessage, autoSave: true);
            await uow.CompleteAsync();

            logger.LogInformation("Message {MessageId} saved to inbox for processing", messageId);
        }
        catch (Exception ex) when (IsDuplicateKeyException(ex))
        {
            // Another pod inserted the same MessageId between our check and insert (unique index).
            // This is expected in multi-pod environments on RabbitMQ redelivery — treat as success.
            logger.LogInformation("Message {MessageId} was concurrently inserted by another pod. Treating as idempotent success.", messageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving message {MessageId} to inbox. Message will be requeued.", messageId);
            consumingChannel.BasicReject(ea.DeliveryTag, requeue: true);
            return;
        }

        // ACK only after successful save to inbox
        consumingChannel.BasicAck(ea.DeliveryTag, multiple: false);
    }

    private static Guid? ResolveTenantId(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            return null;

        if (Guid.TryParse(provider, out var tenantGuid))
            return tenantGuid;

        return null;
    }

    private void CleanupConnection()
    {
        try
        {
            if (_connection != null) _connection.ConnectionShutdown -= OnConnectionShutdown;
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error during connection cleanup");
        }

        _channel = null;
        _connection = null;
    }

    /// <summary>
    /// Detects PostgreSQL unique constraint violation (error code 23505) propagated through EF Core.
    /// This occurs when two pods concurrently insert the same MessageId on RabbitMQ redelivery.
    /// Uses reflection to avoid a direct Npgsql dependency in the Application layer.
    /// </summary>
    private static bool IsDuplicateKeyException(Exception ex)
    {
        var current = ex;
        while (current != null)
        {
            // Npgsql.PostgresException has a SqlState property — check by type name to avoid package reference
            var type = current.GetType();
            if (type.Name == "PostgresException")
            {
                var sqlState = type.GetProperty("SqlState")?.GetValue(current) as string;
                if (sqlState == "23505") return true;
            }
            current = current.InnerException;
        }
        return false;
    }

    public override void Dispose()
    {
        CleanupConnection();
        _reconnectLock.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
