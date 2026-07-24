using System;
using System.Globalization;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public async Task PublishMessageAsync(TQueueMessage message)
        {
            if (EqualityComparer<TQueueMessage>.Default.Equals(message, default))
                throw new ArgumentNullException(nameof(message));

            if (message.TimeToLive.Ticks <= 0)
                throw new QueueingException($"{nameof(message.TimeToLive)} cannot be zero or negative");

            var channel = await _channelProvider.GetChannelAsync();

            try
            {
                message.MessageId = Guid.NewGuid();

                var serializedMessage = SerializeMessage(message);

                var properties = new BasicProperties
                {
                    Persistent = true, // quorum queues persist
                    Type = _queueName,
                    MessageId = message.MessageId.ToString(),
                    Expiration = message.TimeToLive.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)
                };

                // Publisher confirmations are enabled on pooled channels, so BasicPublishAsync
                // awaits the broker confirmation and throws if the message is not confirmed.
                await channel.BasicPublishAsync(
                    exchange: _exchangeName,
                    routingKey: _queueName,
                    mandatory: false,
                    basicProperties: properties,
                    body: serializedMessage
                );

                _logger.LogInformation("Published message {MessageId} to {Queue}", message.MessageId, _queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PublishMessage Exception: {Message}", ex.Message);
                throw new QueueingException($"Publish failed: {ex.Message}", ex);
            }
            finally
            {
                // Return the channel so it is pooled (if still open) or disposed, and its
                // throttling permit is released. Without this the channel and permit leak.
                _channelProvider.ReturnChannel(channel);
            }
        }

        private static byte[] SerializeMessage(TQueueMessage message)
        {
            var stringContent = JsonConvert.SerializeObject(message);
            return Encoding.UTF8.GetBytes(stringContent);
        }
    }
}
