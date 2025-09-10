using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Constants;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public sealed class PooledQueueChannelProvider<TQueueMessage> : IQueueChannelProvider<TQueueMessage>
        where TQueueMessage : IQueueMessage
    {
        private readonly IChannelProvider _channelProvider;
        private readonly ILogger<PooledQueueChannelProvider<TQueueMessage>> _logger;
        private readonly ConcurrentQueue<IModel> _channelPool = new();
        private readonly SemaphoreSlim _channelSemaphore = new(MaxChannels, MaxChannels);
        private readonly Timer _cleanupTimer;
        private readonly string _queueName = typeof(TQueueMessage).Name;
        
        private volatile bool _disposed;
        private volatile bool _queueDeclared;

        private const int MaxChannels = 5000;
        private readonly TimeSpan _channelWaitTimeout = TimeSpan.FromSeconds(10);

        public PooledQueueChannelProvider(
            IChannelProvider channelProvider,
            ILogger<PooledQueueChannelProvider<TQueueMessage>> logger)
        {
            _channelProvider = channelProvider ?? throw new ArgumentNullException(nameof(channelProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _cleanupTimer = new Timer(_ => CleanupIdleChannels(), null,
                TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        public IModel GetChannel()
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(PooledQueueChannelProvider<TQueueMessage>));

            if (!_channelSemaphore.Wait(_channelWaitTimeout))
            {
                throw new TimeoutException(
                    $"Unable to acquire a channel for queue {_queueName} within {_channelWaitTimeout.TotalSeconds} seconds.");
            }

            try
            {
                // Try to get an existing channel
                while (_channelPool.TryDequeue(out var pooled))
                {
                    if (pooled.IsOpen)
                        return pooled;

                    DisposeChannel(pooled);
                }

                // Create new channel
                var channel = _channelProvider.GetChannel() ?? throw new InvalidOperationException("Channel cannot be null.");
                EnsureQueueDeclared(channel);
                return channel;
            }
            catch
            {
                _channelSemaphore.Release();
                throw;
            }
        }

        public void ReturnChannel(IModel channel)
        {
            if (channel?.IsOpen == true && !_disposed)
            {
                _channelPool.Enqueue(channel);
            }
            else
            {
                if (channel != null)
                    DisposeChannel(channel);
            }

            try { _channelSemaphore.Release(); } catch (ObjectDisposedException) { }
        }

        private void EnsureQueueDeclared(IModel channel)
        {
            if (_queueDeclared) return;

            lock (this)
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
                    throw;
                }
            }
        }

        private void DeclareQueue(IModel channel)
        {
            try
            {
                channel.QueueDeclarePassive(_queueName);
                BindToExchange(channel);
            }
            catch (global::RabbitMQ.Client.Exceptions.OperationInterruptedException ex)
            {
                if (ex.ShutdownReason.ReplyCode == 404)
                {
                    CreateQueueWithDeadLetter(channel);
                }
                else if (ex.ShutdownReason.ReplyText.Contains("inequivalent arg"))
                {
                    _logger.LogWarning("Queue {QueueName} exists with incompatible config, using compatibility mode", _queueName);
                    BindToExchange(channel);
                }
                else
                {
                    throw;
                }
            }
        }

        private void CreateQueueWithDeadLetter(IModel channel)
        {
            var dlxName = $"{_queueName}.dlx";
            var dlqName = $"{_queueName}{QueueingConstants.DeadletterAddition}";
            var mainExchange = $"{_queueName}.exchange";

            // Create dead letter setup
            channel.ExchangeDeclare(dlxName, ExchangeType.Direct, durable: true);
            channel.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false, 
                new Dictionary<string, object> { { "x-queue-type", "quorum" }, { "x-overflow", "reject-publish" } });
            channel.QueueBind(dlqName, dlxName, dlqName);

            // Create main queue with dead letter
            channel.ExchangeDeclare(mainExchange, ExchangeType.Direct, durable: true);
            channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false,
                new Dictionary<string, object>
                {
                    { "x-queue-type", "quorum" },
                    { "x-overflow", "reject-publish" },
                    { "x-dead-letter-exchange", dlxName },
                    { "x-dead-letter-routing-key", dlqName },
                    { "x-dead-letter-strategy", "at-least-once" },
                    { "x-delivery-limit", 10 }
                });
            channel.QueueBind(_queueName, mainExchange, _queueName);
        }

        private void BindToExchange(IModel channel)
        {
            var mainExchange = $"{_queueName}.exchange";
            channel.ExchangeDeclare(mainExchange, ExchangeType.Direct, durable: true);
            channel.QueueBind(_queueName, mainExchange, _queueName);
        }

        private void DisposeChannel(IModel channel)
        {
            if (channel == null) return;

            try
            {
                if (channel.IsOpen) channel.Close();
                channel.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error disposing channel");
            }
        }

        private void CleanupIdleChannels()
        {
            if (_disposed) return;

            var channels = new List<IModel>();
            while (_channelPool.TryDequeue(out var channel))
                channels.Add(channel);

            foreach (var channel in channels)
            {
                if (channel.IsOpen)
                {
                    _channelPool.Enqueue(channel);
                }
                else
                {
                    DisposeChannel(channel);
                    try { _channelSemaphore.Release(); } catch (ObjectDisposedException) { }
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cleanupTimer?.Dispose();

            while (_channelPool.TryDequeue(out var channel))
                DisposeChannel(channel);

            _channelSemaphore.Dispose();
        }

        public string QueueName => _queueName;
    }
}