using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Constants;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;
using System.Threading;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public class QueueChannelProvider<TQueueMessage>(
        IChannelProvider channelProvider,
        ILogger<QueueChannelProvider<TQueueMessage>> logger
    ) : IQueueChannelProvider<TQueueMessage>
        where TQueueMessage : IQueueMessage
    {
        private readonly IChannelProvider _channelProvider = channelProvider ?? throw new ArgumentNullException(nameof(channelProvider));
        private readonly ILogger<QueueChannelProvider<TQueueMessage>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ThreadLocal<IModel?> _threadLocalChannel = new();
        private bool _disposed;
        private readonly string _queueName = typeof(TQueueMessage).Name;

        public IModel GetChannel()
        {
            ObjectDisposedException.ThrowIf(_disposed, typeof(QueueChannelProvider<TQueueMessage>));

            if (_threadLocalChannel.Value == null || !_threadLocalChannel.Value.IsOpen)
            {
                _threadLocalChannel.Value?.Dispose();
                var channel = _channelProvider.GetChannel() ?? throw new InvalidOperationException("Channel cannot be null.");
                DeclareQueueAndDeadLetter(channel);
                _threadLocalChannel.Value = channel;
            }

            return _threadLocalChannel.Value!;
        }
        
        private void DeclareQueueAndDeadLetter(IModel channel)
        {
            try
            {
                try
                {
                    channel.QueueDeclarePassive(_queueName);
                    DeclareCompatibleQueue(channel);
                }
                catch (global::RabbitMQ.Client.Exceptions.OperationInterruptedException ex)
                {
                    if (ex.ShutdownReason.ReplyCode == 404)
                    {
                        DeclareQueueWithDeadLetter(channel);
                    }
                    else if (ex.ShutdownReason.ReplyText.Contains("inequivalent arg"))
                    {
                        _logger.LogDebug(ex, "Queue {QueueName} exists with incompatible configuration, falling back to compatibility mode.", _queueName);
                        DeclareCompatibleQueue(channel);
                    }
                    else
                    {
                        throw;
                    }
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
            if (_disposed) return;

            foreach (var channel in _threadLocalChannel.Values)
            {
                try
                {
                    if (channel != null && channel.IsOpen)
                        _channelProvider.ReturnChannel(channel);
                    else
                        channel?.Dispose();
                }
                catch
                {
                    channel?.Dispose();
                }
            }

            _threadLocalChannel.Dispose();
            _disposed = true;
        }

        public string QueueName => _queueName;
    }
}