using System;
using System.Globalization;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Exceptions;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public class QueueProducer<TQueueMessage> : IQueueProducer<TQueueMessage>
        where TQueueMessage : IQueueMessage
    {
        private readonly ILogger<QueueProducer<TQueueMessage>> _logger;
        private readonly IQueueChannelProvider<TQueueMessage> _channelProvider;
        private readonly string _queueName;
        private readonly string _exchangeName;

        public QueueProducer(
            IQueueChannelProvider<TQueueMessage> channelProvider,
            ILogger<QueueProducer<TQueueMessage>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _channelProvider = channelProvider ?? throw new ArgumentNullException(nameof(channelProvider));
            _queueName = typeof(TQueueMessage).Name;
            _exchangeName = $"{_queueName}.exchange";
        }

        public void PublishMessage(TQueueMessage message)
        {
            if (EqualityComparer<TQueueMessage>.Default.Equals(message, default))
                throw new ArgumentNullException(nameof(message));

            if (message.TimeToLive.Ticks <= 0)
                throw new QueueingException($"{nameof(message.TimeToLive)} cannot be zero or negative");

            var channel = _channelProvider.GetChannel();

            try
            {
                message.MessageId = Guid.NewGuid();

                var serializedMessage = SerializeMessage(message);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true; // quorum queues persist
                properties.Type = _queueName;
                properties.MessageId = message.MessageId.ToString();
                properties.Expiration = message.TimeToLive.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);

                // Enable publisher confirms once per channel
                channel.ConfirmSelect();

                channel.BasicPublish(
                    exchange: _exchangeName,
                    routingKey: _queueName,
                    basicProperties: properties,
                    body: serializedMessage
                );

                // Wait for confirmation
                if (!channel.WaitForConfirms(TimeSpan.FromSeconds(5)))
                {
                    throw new QueueingException($"Publish failed: broker did not confirm message {message.MessageId}");
                }

                _logger.LogInformation("Published message {MessageId} to {Queue}", message.MessageId, _queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PublishMessage Exception: {Message}", ex.Message);
                throw new QueueingException($"Publish failed: {ex.Message}", ex);
            }
        }

        private static byte[] SerializeMessage(TQueueMessage message)
        {
            var stringContent = JsonConvert.SerializeObject(message);
            return Encoding.UTF8.GetBytes(stringContent);
        }
    }
}
