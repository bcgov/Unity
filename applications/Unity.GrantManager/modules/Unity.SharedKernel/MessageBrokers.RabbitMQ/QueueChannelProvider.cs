using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Constants;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;
using System.Threading;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public class QueueChannelProvider<TQueueMessage>(IChannelProvider channelProvider, ILogger<QueueChannelProvider<TQueueMessage>> logger) : IQueueChannelProvider<TQueueMessage>
        where TQueueMessage : IQueueMessage
    {
        private readonly IChannelProvider _channelProvider = channelProvider ?? throw new ArgumentNullException(nameof(channelProvider));
        private readonly ILogger<QueueChannelProvider<TQueueMessage>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly Lock _lock = new();
        private IModel? _channel;
        private bool _disposed;
        private bool _queuesDeclared;
        private readonly string _queueName = typeof(TQueueMessage).Name;

        public IModel GetChannel()
        {
            ObjectDisposedException.ThrowIf(_disposed, typeof(QueueChannelProvider<TQueueMessage>));

            lock (_lock)
            {
                if (_channel == null || !_channel.IsOpen)
                {
                    _channel?.Dispose();
                    _channel = _channelProvider.GetChannel();
                    _queuesDeclared = false;
                }

                if (_channel == null || !_channel.IsOpen)
                    throw new InvalidOperationException("Failed to get a valid RabbitMQ channel");

                if (!_queuesDeclared)
                {
                    DeclareQueueAndDeadLetter(_channel);
                    _queuesDeclared = true;
                }

                return _channel;
            }
        }

        private void DeclareQueueAndDeadLetter(IModel channel)
        {
            try
            {
                // First, try to declare the queue as passive to check if it exists
                try
                {
                    channel.QueueDeclarePassive(_queueName);
                    // Queue exists and is compatible, just declare exchange and binding
                    DeclareCompatibleQueue(channel);
                    return;
                }
                catch (global::RabbitMQ.Client.Exceptions.OperationInterruptedException ex)
                {
                    // The channel is now closed. Get a new one immediately.
                    _channel?.Dispose();
                    _channel = _channelProvider.GetChannel();
                    channel = _channel ?? throw new InvalidOperationException("Failed to get a new RabbitMQ channel after an error.");

                    // Check the reason for the exception
                    if (ex.ShutdownReason.ReplyCode == 404)
                    {
                        // Queue not found, declare it with the full dead-letter configuration
                        DeclareQueueWithDeadLetter(channel);
                        return;
                    }
                    if (ex.ShutdownReason.ReplyText.Contains("inequivalent arg"))
                    {
                        _logger.LogDebug(ex, "Queue {QueueName} exists with incompatible configuration, falling back to compatibility mode.", _queueName);
                        DeclareCompatibleQueue(channel);
                        return;
                    }

                    // Re-throw any other exceptions
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to declare queues for {_queueName}", ex);
            }
        }

        private void DeclareQueueWithDeadLetter(IModel channel)
        {
            var dlxName = $"{_queueName}.dlx";
            var dlqName = $"{_queueName}{QueueingConstants.DeadletterAddition}";
            var mainExchange = $"{_queueName}.exchange";

            channel.ExchangeDeclare(dlxName, ExchangeType.Direct, durable: true);

            var dlqArgs = new Dictionary<string, object>
            {
                { "x-queue-type", "quorum" },
                { "x-overflow", "reject-publish" }
            };

            channel.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false, arguments: dlqArgs);
            channel.QueueBind(dlqName, dlxName, dlqName);

            channel.ExchangeDeclare(mainExchange, ExchangeType.Direct, durable: true);

            var mainQArgs = new Dictionary<string, object>
            {
                { "x-queue-type", "quorum" },
                { "x-overflow", "reject-publish" },
                { "x-dead-letter-exchange", dlxName },
                { "x-dead-letter-routing-key", dlqName },
                { "x-dead-letter-strategy", "at-least-once" },
                { "x-delivery-limit", 10 }
            };

            channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false, arguments: mainQArgs);
            channel.QueueBind(_queueName, mainExchange, _queueName);
        }

        private void DeclareCompatibleQueue(IModel channel)
        {
            var mainExchange = $"{_queueName}.exchange";

            try
            {
                channel.ExchangeDeclare(mainExchange, ExchangeType.Direct, durable: true);
                channel.QueueBind(_queueName, mainExchange, _queueName);

                _logger.LogWarning("Queue {QueueName} exists with incompatible configuration. Running in compatibility mode without dead letter support.", _queueName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to declare queue {_queueName} in compatibility mode. " +
                    "The existing queue has incompatible configuration and cannot be used.", ex);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing && _channel != null && _channelProvider != null)
            {
                try
                {
                    _channelProvider.ReturnChannel(_channel);
                }
                catch
                {
                    _channel?.Dispose();
                }
            }        
            _disposed = true;
            _channel = null;
        }

        public string QueueName => _queueName;
    }
}
