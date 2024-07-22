using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Unity.RabbitMQ.Constants;
using Unity.RabbitMQ.Interfaces;

namespace Unity.RabbitMQ
{
    public class QueueChannelProvider<TQueueMessage> : IQueueChannelProvider<TQueueMessage> where TQueueMessage : IQueueMessage
    {
        private readonly IChannelProvider _channelProvider;
        private IModel? _channel;
        private readonly string _queueName;
        private readonly ILogger<QueueChannelProvider<TQueueMessage>> _logger;

        public QueueChannelProvider(
            IChannelProvider channelProvider,
            ILogger<QueueChannelProvider<TQueueMessage>> logger)
        {
            _channelProvider = channelProvider;
            _queueName = typeof(TQueueMessage).Name;
            _logger = logger;
        }

        public IModel? GetChannel()
        {
            _channel = _channelProvider.GetChannel();
            DeclareQueueAndDeadLetter();
            return _channel;
        }

        private void DeclareQueueAndDeadLetter()
        {
            var deadLetterQueueName = $"{_queueName}{QueueingConstants.DeadletterAddition}";

            // Declare the DeadLetter Queue
            var deadLetterQueueArgs = new Dictionary<string, object>
            {
                { "x-queue-type", "quorum" }, 
                { "overflow", "reject-publish" } // If the queue is full, reject the publish
            };
            if(_channel == null) return;

            _channel.ExchangeDeclare(deadLetterQueueName, ExchangeType.Direct);
            _channel.QueueDeclare(deadLetterQueueName, true, false, false, deadLetterQueueArgs);
            _channel.QueueBind(deadLetterQueueName, deadLetterQueueName, deadLetterQueueName, null);

            // Declare the Queue
            var queueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", deadLetterQueueName },
                { "x-dead-letter-routing-key", deadLetterQueueName },
                { "x-queue-type", "quorum" },
                { "x-dead-letter-strategy", "at-least-once" }, // Ensure that deadletter messages are delivered in any case see: https://www.rabbitmq.com/quorum-queues.html#dead-lettering
                { "overflow", "reject-publish" } // If the queue is full, reject the publish
            };

            _channel.ExchangeDeclare(_queueName, ExchangeType.Direct);
            _channel.QueueDeclare(_queueName, true, false, false, queueArgs);
            _channel.QueueBind(_queueName, _queueName, _queueName, null);
        }

        public void Dispose()
        {
            // This dispose is required but is done in the channel provider
        }
    }
}
