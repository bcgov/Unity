using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Exceptions;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Constants;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;
using Unity.Modules.Shared.Utils;
using Volo.Abp.Auditing;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Claims;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public class QueueConsumerHandler<TMessageConsumer, TQueueMessage>(
        IServiceProvider serviceProvider,
        ILogger<QueueConsumerHandler<TMessageConsumer, TQueueMessage>> logger)
        : IQueueConsumerHandler<TMessageConsumer, TQueueMessage>
        where TMessageConsumer : IQueueConsumer<TQueueMessage>
        where TQueueMessage : class, IQueueMessage
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        private readonly ILogger<QueueConsumerHandler<TMessageConsumer, TQueueMessage>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly string _queueName = typeof(TQueueMessage).Name;
        private string? _consumerTag;
        private readonly string _consumerName = typeof(TMessageConsumer).Name;

        public async Task RegisterQueueConsumerAsync()
        {
            _logger.LogInformation("Registering {Consumer} as a consumer for Queue {Queue}", _consumerName, _queueName);

            using var scope = _serviceProvider.CreateScope();
            var channelProvider = scope.ServiceProvider.GetRequiredService<IQueueChannelProvider<TQueueMessage>>();
            var consumerChannel = await channelProvider.GetChannelAsync() ?? throw new QueueingException($"Failed to create consumer channel for {_queueName}");
            var consumer = new AsyncEventingBasicConsumer(consumerChannel);
            consumer.ReceivedAsync += HandleMessageAsync;

            try
            {
                _consumerTag = await consumerChannel.BasicConsumeAsync(
                    queue: _queueName,
                    autoAck: false,
                    consumerTag: string.Empty,
                    noLocal: false,
                    exclusive: false,
                    arguments: null,
                    consumer: consumer);

                _logger.LogInformation("Successfully registered {Consumer} as consumer for {Queue}", _consumerName, _queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BasicConsume failed for Queue '{Queue}'", _queueName);
                throw new QueueingException($"BasicConsume failed for Queue '{_queueName}'", ex);
            }
        }

        async Task IQueueConsumerHandler<TMessageConsumer, TQueueMessage>.CancelQueueConsumerAsync()
        {
            if (string.IsNullOrEmpty(_consumerTag))
                return;

            _logger.LogInformation("Canceling consumer {Consumer} for Queue {Queue}", _consumerName, _queueName);

            using var scope = _serviceProvider.CreateScope();
            var channelProvider = scope.ServiceProvider.GetRequiredService<IQueueChannelProvider<TQueueMessage>>();
            var channel = await channelProvider.GetChannelAsync();

            try
            {
                await channel.BasicCancelAsync(_consumerTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling consumer {Consumer}", _consumerName);
                throw new QueueingException($"Error canceling consumer {_consumerName}", ex);
            }
            finally
            {
                // Return the short-lived cancel channel so it is disposed and its permit released.
                channelProvider.ReturnChannel(channel);
            }
        }

        private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs ea)
        {
            _logger.LogInformation("Received message on {Queue}", _queueName);

            using var consumerScope = _serviceProvider.CreateScope();
            var consumingChannel = ((AsyncEventingBasicConsumer)sender).Channel;

            try
            {
                var message = DeserializeMessage(ea.Body.ToArray())
                              ?? throw new QueueingException("Failed to deserialize message");

                _logger.LogInformation("Processing MessageId {MessageId}", message.MessageId);

                if (message is not ITenantedQueueMessage tenantedMessage)
                {
                    var consumerInstance = consumerScope.ServiceProvider.GetRequiredService<TMessageConsumer>();
                    await consumerInstance.ConsumeAsync(message);
                }
                else if (tenantedMessage.TenantId == Guid.Empty)
                {
                    _logger.LogError("Message {MessageId} on {Queue} has an empty TenantId and cannot be processed", message.MessageId, _queueName);
                    await consumingChannel.BasicRejectAsync(ea.DeliveryTag, requeue: false);
                    return;
                }
                else
                {
                    await ConsumeWithAuditingAsync(consumerScope, tenantedMessage, message);
                }

                await consumingChannel.BasicAckAsync(ea.DeliveryTag, multiple: false);

                _logger.LogInformation("Message {MessageId} successfully processed", message.MessageId);
            }
            catch (JsonException jex)
            {
                _logger.LogError(jex, "Deserialization failed for message on {Queue}", _queueName);
                await consumingChannel.BasicRejectAsync(ea.DeliveryTag, requeue: false);
            }
            catch (Exception ex)
            {
                var retryCount = GetRetryCount(ea.BasicProperties.Headers);
                if (IsPermanentFailure(ex) || retryCount >= QueueingConstants.MaxMessageRetries)
                {
                    _logger.LogError(ex,
                        "Permanently rejecting message {MessageId} on {Queue} after {RetryCount} retries",
                        ea.BasicProperties.MessageId,
                        _queueName,
                        retryCount);
                    await consumingChannel.BasicRejectAsync(ea.DeliveryTag, requeue: false);
                    return;
                }

                var nextRetryCount = retryCount + 1;
                var delay = QueueingConstants.RetryDelays[retryCount];
                var channelProvider = consumerScope.ServiceProvider
                    .GetRequiredService<IQueueChannelProvider<TQueueMessage>>();

                _logger.LogWarning(ex,
                    "Scheduling retry {RetryCount} for message {MessageId} on {Queue} in {Delay}",
                    nextRetryCount,
                    ea.BasicProperties.MessageId,
                    _queueName,
                    delay);

                var retryAccepted = await channelProvider.PublishRetryAsync(consumingChannel, ea, nextRetryCount, delay);
                if (retryAccepted)
                {
                    await consumingChannel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                else
                {
                    // Retry publish failed; the provider requeued the original delivery or arranged channel recovery.
                    // Do not acknowledge the original delivery here so it will be redelivered.
                }
            }
        }

        private static int GetRetryCount(IDictionary<string, object?>? headers)
        {
            if (headers == null || !headers.TryGetValue(QueueingConstants.RetryCountHeader, out var value))
            {
                return 0;
            }

            return value switch
            {
                int count => Math.Max(count, 0),
                long count => Math.Max((int)count, 0),
                byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var count) => Math.Max(count, 0),
                _ when int.TryParse(value?.ToString(), out var count) => Math.Max(count, 0),
                _ => 0
            };
        }

        private static bool IsPermanentFailure(Exception exception)
        {
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is ArgumentException ||
                    current is Volo.Abp.Validation.AbpValidationException ||
                    current is Volo.Abp.Domain.Entities.EntityNotFoundException)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Wraps consumer execution in a background-job auditing scope, mirroring the way
        /// ASP.NET Core middleware wraps controller actions. Tenant context, identity, and
        /// audit persistence are handled here so individual consumers stay free of
        /// infrastructure concerns.
        /// </summary>
        private async Task ConsumeWithAuditingAsync(IServiceScope consumerScope, ITenantedQueueMessage tenantedMessage, TQueueMessage message)
        {
            var auditingManager = consumerScope.ServiceProvider.GetRequiredService<IAuditingManager>();
            var principalAccessor = consumerScope.ServiceProvider.GetRequiredService<ICurrentPrincipalAccessor>();
            var currentTenant = consumerScope.ServiceProvider.GetRequiredService<ICurrentTenant>();
            var auditingStore = consumerScope.ServiceProvider.GetRequiredService<IAuditingStore>();

            using (BackgroundJobExecutionContext.Use())
            using (BackgroundJobContext.Set(auditingManager, principalAccessor, currentTenant, tenantedMessage.TenantId))
            {
                AddConsumerAuditAction(auditingManager, message);

                var consumerInstance = consumerScope.ServiceProvider.GetRequiredService<TMessageConsumer>();
                await consumerInstance.ConsumeAsync(message);

                // Persist audit log if the consumer produced any entity changes.
                // Entity changes are collected by AbpDbContext.SaveChangesAsync() during UOW commit,
                // so this call happens after the consumer's unit of work completes.
                if (auditingManager.Current?.Log is { EntityChanges.Count: > 0 } log)
                {
                    try
                    {
                        await auditingStore.SaveAsync(log);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to persist audit log for message {MessageId}; consumer work was already committed.",
                            message.MessageId);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a single <see cref="AuditLogActionInfo"/> to the current audit scope using
        /// reflection on the generic consumer type. ABP requires at least one recorded action
        /// before it will persist an audit log with entity changes.
        /// </summary>
        private static void AddConsumerAuditAction(IAuditingManager auditingManager, TQueueMessage message)
        {
            if (auditingManager.Current?.Log == null)
            {
                return;
            }

            auditingManager.Current.Log.Actions.Add(new AuditLogActionInfo
            {
                ServiceName = typeof(TMessageConsumer).FullName ?? typeof(TMessageConsumer).Name,
                MethodName = nameof(IQueueConsumer<TQueueMessage>.ConsumeAsync),
                Parameters = System.Text.Json.JsonSerializer.Serialize(message),
                ExecutionTime = DateTime.UtcNow
            });
        }

        private static TQueueMessage? DeserializeMessage(byte[] body)
        {
            var json = Encoding.UTF8.GetString(body);
            return JsonConvert.DeserializeObject<TQueueMessage>(json);
        }
    }
}
