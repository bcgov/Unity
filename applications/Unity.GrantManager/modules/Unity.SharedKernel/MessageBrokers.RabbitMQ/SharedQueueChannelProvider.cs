using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Constants;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public class SharedQueueChannelProvider<TMessage> : IQueueChannelProvider<TMessage>
        where TMessage : class, IQueueMessage
    {
        private readonly IChannelProvider _channelProvider;
        private readonly ILogger<SharedQueueChannelProvider<TMessage>> _logger;
        private readonly string _queueName = typeof(TMessage).Name;
        private volatile bool _queueDeclared;
        private readonly object _queueDeclareLock = new();

        public SharedQueueChannelProvider(
            IChannelProvider channelProvider,
            ILogger<SharedQueueChannelProvider<TMessage>> logger)
        {
            _channelProvider = channelProvider ?? throw new ArgumentNullException(nameof(channelProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IModel GetChannel()
        {
            var channel = _channelProvider.GetChannel() ?? throw new InvalidOperationException("Channel provider returned null.");
            EnsureQueueDeclared(channel);
            return channel;
        }

        private void EnsureQueueDeclared(IModel channel)
        {
            if (_queueDeclared) return;

            lock (_queueDeclareLock)
            {
                if (_queueDeclared) return;

                try
                {
                    DeclareQueue(channel);
                    _queueDeclared = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to declare queue {QueueName}", _queueName);
                    throw new InvalidOperationException($"Failed to declare queue '{_queueName}'. See inner exception for details.", ex);
                }
            }
        }

        private void DeclareQueue(IModel channel)
        {
            try
            {
                var dlxName = $"{_queueName}.dlx";
                var dlqName = $"{_queueName}{QueueingConstants.DeadletterAddition}";

                // Ensure DLX exchange exists
                channel.ExchangeDeclare(dlxName, ExchangeType.Direct, durable: true);

                // Ensure DLQ exists and is bound to DLX
                channel.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false,
                    arguments: new Dictionary<string, object>
                    {
                        { "x-queue-type", "quorum" },
                        { "x-overflow", "reject-publish" }
                    });
                channel.QueueBind(dlqName, dlxName, dlqName);

                // Declare main queue with DLX args
                channel.QueueDeclare(
                    _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: new Dictionary<string, object>
                    {
                        { "x-queue-type", "quorum" },
                        { "x-overflow", "reject-publish" },
                        { "x-dead-letter-exchange", dlxName },
                        { "x-dead-letter-routing-key", dlqName },
                        { "x-dead-letter-strategy", "at-least-once" },
                        { "x-delivery-limit", 10 }
                    });

                BindToExchange(channel);
            }
            catch (global::RabbitMQ.Client.Exceptions.OperationInterruptedException ex)
            {
                if (ex.ShutdownReason.ReplyCode == 406 &&
                    ex.ShutdownReason.ReplyText.Contains("inequivalent arg"))
                {
                    _logger.LogWarning(
                        ex,
                        "Queue {QueueName} exists with incompatible config. Using existing queue in compatibility mode.",
                        _queueName);

                    BindToExchange(channel);
                }
                else
                {
                    throw;
                }
            }
        }

        private void BindToExchange(IModel channel)
        {
            var mainExchange = $"{_queueName}.exchange";
            channel.ExchangeDeclare(mainExchange, ExchangeType.Direct, durable: true);
            channel.QueueBind(_queueName, mainExchange, _queueName);
        }

        public void Dispose()
        {
            // Channel is managed by SharedChannelProvider, so we don't dispose it here
        }
    }
}