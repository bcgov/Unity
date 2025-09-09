using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Exceptions;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public class QueueConsumerHandler<TMessageConsumer, TQueueMessage>
        : IQueueConsumerHandler<TMessageConsumer, TQueueMessage>
        where TMessageConsumer : IQueueConsumer<TQueueMessage>
        where TQueueMessage : class, IQueueMessage
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<QueueConsumerHandler<TMessageConsumer, TQueueMessage>> _logger;
        private readonly string _queueName;
        private IModel? _consumerChannel;
        private string? _consumerTag;
        private readonly string _consumerName;

        public QueueConsumerHandler(
            IServiceProvider serviceProvider,
            ILogger<QueueConsumerHandler<TMessageConsumer, TQueueMessage>> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queueName = typeof(TQueueMessage).Name;
            _consumerName = typeof(TMessageConsumer).Name;
        }

        public void RegisterQueueConsumer()
        {
            _logger.LogInformation("Registering {Consumer} as a consumer for Queue {Queue}", _consumerName, _queueName);

            var scope = _serviceProvider.CreateScope();
            _consumerChannel = scope.ServiceProvider
                .GetRequiredService<IQueueChannelProvider<TQueueMessage>>()
                .GetChannel();

            if (_consumerChannel == null)
            {
                throw new QueueingException($"Failed to create consumer channel for {_queueName}");
            }

            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
            consumer.Received += HandleMessage;

            try
            {
                _consumerTag = _consumerChannel.BasicConsume(
                    queue: _queueName,
                    autoAck: false,
                    consumer: consumer);

                _logger.LogInformation("Successfully registered {Consumer} as consumer for {Queue}", _consumerName, _queueName);
            }
            catch (Exception ex)
            {
                var msg = $"BasicConsume failed for Queue '{_queueName}'";
                _logger.LogError(ex, msg);
                throw new QueueingException(msg, ex);
            }
        }

        void IQueueConsumerHandler<TMessageConsumer, TQueueMessage>.CancelQueueConsumer()
        {
            if (_consumerChannel == null || string.IsNullOrEmpty(_consumerTag))
                return;

            _logger.LogInformation("Canceling consumer {Consumer} for Queue {Queue}", _consumerName, _queueName);

            try
            {
                _consumerChannel.BasicCancel(_consumerTag);
            }
            catch (Exception ex)
            {
                var msg = $"Error canceling consumer {_consumerName}";
                _logger.LogError(ex, msg);
                throw new QueueingException(msg, ex);
            }
            finally
            {
                _consumerChannel.Close();
                _consumerChannel.Dispose();
                _consumerChannel = null;
            }
        }

        private async Task HandleMessage(object sender, BasicDeliverEventArgs ea)
        {
            _logger.LogInformation("Received message on {Queue}", _queueName);

            using var consumerScope = _serviceProvider.CreateScope();
            var consumingChannel = ((AsyncEventingBasicConsumer)sender).Model;

            IModel? producingChannel = null;
            try
            {
                producingChannel = consumerScope.ServiceProvider.GetRequiredService<IChannelProvider>().GetChannel();
                if (producingChannel == null)
                    throw new QueueingException("Failed to acquire producing channel");

                var message = DeserializeMessage(ea.Body.ToArray());
                if (message == null)
                    throw new QueueingException("Failed to deserialize message");

                _logger.LogInformation("Processing MessageId {MessageId}", message.MessageId);

                producingChannel.TxSelect();

                var consumerInstance = consumerScope.ServiceProvider.GetRequiredService<TMessageConsumer>();

                await consumerInstance.ConsumeAsync(message);

                if (producingChannel.IsClosed || consumingChannel.IsClosed)
                    throw new QueueingException("A channel is closed during processing");

                producingChannel.TxCommit();
                consumingChannel.BasicAck(ea.DeliveryTag, multiple: false);

                _logger.LogInformation("Message {MessageId} successfully processed", message.MessageId);
            }
            catch (JsonException jex)
            {
                _logger.LogError(jex, "Deserialization failed for message on {Queue}", _queueName);
                consumingChannel.BasicReject(ea.DeliveryTag, requeue: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message on {Queue}", _queueName);
                if (producingChannel != null)
                {
                    RejectMessage(ea.DeliveryTag, consumingChannel, producingChannel);
                }
            }
        }

        private void RejectMessage(ulong deliveryTag, IModel consumeChannel, IModel scopeChannel)
        {
            try
            {
                scopeChannel.TxRollback();
                _logger.LogInformation("Rolled back producing transaction");

                consumeChannel.BasicReject(deliveryTag, requeue: false);
                _logger.LogWarning("Message rejected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during message rejection");
            }
        }

        private static TQueueMessage? DeserializeMessage(byte[] body)
        {
            var json = Encoding.UTF8.GetString(body);
            return JsonConvert.DeserializeObject<TQueueMessage>(json);
        }
    }
}
