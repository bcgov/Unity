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

        public void RegisterQueueConsumer()
        {
            _logger.LogInformation("Registering {Consumer} as a consumer for Queue {Queue}", _consumerName, _queueName);

            using var scope = _serviceProvider.CreateScope();
            var channelProvider = scope.ServiceProvider.GetRequiredService<IQueueChannelProvider<TQueueMessage>>();
            var consumerChannel = channelProvider.GetChannel() ?? throw new QueueingException($"Failed to create consumer channel for {_queueName}");
            var consumer = new AsyncEventingBasicConsumer(consumerChannel);
            consumer.Received += HandleMessage;

            try
            {
                _consumerTag = consumerChannel.BasicConsume(
                    queue: _queueName,
                    autoAck: false,
                    consumer: consumer);

                _logger.LogInformation("Successfully registered {Consumer} as consumer for {Queue}", _consumerName, _queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BasicConsume failed for Queue '{Queue}'", _queueName);
                throw new QueueingException($"BasicConsume failed for Queue '{_queueName}'", ex);
            }
        }

        void IQueueConsumerHandler<TMessageConsumer, TQueueMessage>.CancelQueueConsumer()
        {
            if (string.IsNullOrEmpty(_consumerTag))
                return;

            _logger.LogInformation("Canceling consumer {Consumer} for Queue {Queue}", _consumerName, _queueName);

            using var scope = _serviceProvider.CreateScope();
            var channelProvider = scope.ServiceProvider.GetRequiredService<IQueueChannelProvider<TQueueMessage>>();
            var channel = channelProvider.GetChannel();

            try
            {
                channel.BasicCancel(_consumerTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling consumer {Consumer}", _consumerName);
                throw new QueueingException($"Error canceling consumer {_consumerName}", ex);
            }
        }

        private async Task HandleMessage(object sender, BasicDeliverEventArgs ea)
        {
            _logger.LogInformation("Received message on {Queue}", _queueName);

            using var consumerScope = _serviceProvider.CreateScope();
            var consumingChannel = ((AsyncEventingBasicConsumer)sender).Model;

            try
            {
                var message = DeserializeMessage(ea.Body.ToArray())
                              ?? throw new QueueingException("Failed to deserialize message");

                _logger.LogInformation("Processing MessageId {MessageId}", message.MessageId);

                var consumerInstance = consumerScope.ServiceProvider.GetRequiredService<TMessageConsumer>();
                await consumerInstance.ConsumeAsync(message);

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
                consumingChannel.BasicReject(ea.DeliveryTag, requeue: false);
            }
        }

        private static TQueueMessage? DeserializeMessage(byte[] body)
        {
            var json = Encoding.UTF8.GetString(body);
            return JsonConvert.DeserializeObject<TQueueMessage>(json);
        }
    }
}
