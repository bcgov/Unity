using System.Text;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Unity.RabbitMQ.Interfaces;
using Unity.RabbitMQ.Exceptions;

namespace Unity.RabbitMQ
{
    public class QueueProducer<TQueueMessage> : IQueueProducer<TQueueMessage> where TQueueMessage : IQueueMessage
    {
        private readonly ILogger<QueueProducer<TQueueMessage>> _logger;
        private readonly string? _queueName;
        private readonly IModel? _channel;

        public QueueProducer(IQueueChannelProvider<TQueueMessage> channelProvider, ILogger<QueueProducer<TQueueMessage>> logger)
        {
             _logger = logger;

            try{
                _channel = channelProvider?.GetChannel();
                _queueName = typeof(TQueueMessage).Name;
            } catch (Exception ex) {
                var ExceptionMessage = ex.Message;
                _logger.LogError(ex, "QueueProducer Constructor issue: {ExceptionMessage}", ExceptionMessage);
            }

        }

        public void PublishMessage(TQueueMessage message)
        {
            if (Equals(message, default(TQueueMessage))) throw new ArgumentNullException(nameof(message));
            if (message.TimeToLive.Ticks <= 0) throw new QueueingException($"{nameof(message.TimeToLive)} cannot be zero or negative");
            if (_channel == null) throw new QueueingException("QueueProducer -> PublishMessage: Null Channel");
            try
            {
                message.MessageId = Guid.NewGuid();
                var serializedMessage = SerializeMessage(message);
                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.Type = _queueName;
                properties.Expiration = message.TimeToLive.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);

                _channel.BasicPublish(_queueName, _queueName, properties, serializedMessage);
            }
            catch (Exception ex)
            {
                var PublishMessageException = ex.Message;
                _logger.LogError(ex, "PublishMessage Exception: {PublishMessageException}", PublishMessageException);
                throw new QueueingException(PublishMessageException);
            }
        }

        private static byte[] SerializeMessage(TQueueMessage message)
        {
            var stringContent = JsonConvert.SerializeObject(message);
            return Encoding.UTF8.GetBytes(stringContent);
        }
    }
}
