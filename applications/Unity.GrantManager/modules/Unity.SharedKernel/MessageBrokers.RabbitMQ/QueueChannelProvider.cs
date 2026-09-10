using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly ConcurrentQueue<IChannel> _channelPool = new();
        private readonly SemaphoreSlim _channelSemaphore = new(MaxChannels, MaxChannels);
        private readonly Timer _cleanupTimer;
        private readonly string _queueName = typeof(TQueueMessage).Name;

        private volatile bool _disposed;
        private volatile bool _queueDeclared;
        private readonly SemaphoreSlim _queueDeclareLock = new(1, 1);

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

        public async Task<IChannel> GetChannelAsync()
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(PooledQueueChannelProvider<TQueueMessage>));

            if (!await _channelSemaphore.WaitAsync(_channelWaitTimeout))
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
                var channel = await _channelProvider.GetChannelAsync() ?? throw new InvalidOperationException("Channel cannot be null.");
                await EnsureQueueDeclaredAsync(channel);
                return channel;
            }
            catch
            {
                _channelSemaphore.Release();
                throw;
            }
        }

        public void ReturnChannel(IChannel channel)
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

            try
            {
                _channelSemaphore.Release();
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogWarning(ex, "Attempted to release a disposed semaphore in ReturnChannel.");
            }
        }

        private async Task EnsureQueueDeclaredAsync(IChannel channel)
        {
            if (_queueDeclared) return;

            await _queueDeclareLock.WaitAsync();
            try
            {
                if (_queueDeclared) return;

                await DeclareQueueAsync(channel);
                _queueDeclared = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to declare queue {QueueName}", _queueName);
                throw new InvalidOperationException($"Failed to declare queue '{_queueName}'. See inner exception for details.", ex);
            }
            finally
            {
                _queueDeclareLock.Release();
            }
        }

        private async Task DeclareQueueAsync(IChannel channel)
        {
            try
            {
                var dlxName = $"{_queueName}.dlx";
                var dlqName = $"{_queueName}{QueueingConstants.DeadletterAddition}";

                // Ensure DLX exchange exists
                await channel.ExchangeDeclareAsync(dlxName, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);

                // Ensure DLQ exists and is bound to DLX
                await channel.QueueDeclareAsync(dlqName, durable: true, exclusive: false, autoDelete: false,
                    arguments: new Dictionary<string, object?>
                    {
                        { "x-queue-type", "quorum" },
                        { "x-overflow", "reject-publish" }
                    });
                await channel.QueueBindAsync(dlqName, dlxName, dlqName, arguments: null);

                // Declare main queue with DLX args
                await channel.QueueDeclareAsync(
                    _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: new Dictionary<string, object?>
                    {
                        { "x-queue-type", "quorum" },
                        { "x-overflow", "reject-publish" },
                        { "x-dead-letter-exchange", dlxName },
                        { "x-dead-letter-routing-key", dlqName },
                        { "x-dead-letter-strategy", "at-least-once" },
                        { "x-delivery-limit", 10 }
                    });

                await BindToExchangeAsync(channel);
            }
            catch (global::RabbitMQ.Client.Exceptions.OperationInterruptedException ex)
            {
                if (ex.ShutdownReason?.ReplyCode == 406 &&
                    ex.ShutdownReason.ReplyText.Contains("inequivalent arg"))
                {
                    _logger.LogWarning(
                        ex,
                        "Queue {QueueName} exists with incompatible config. Using existing queue in compatibility mode.",
                        _queueName);

                    await BindToExchangeAsync(channel);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task BindToExchangeAsync(IChannel channel)
        {
            var mainExchange = $"{_queueName}.exchange";
            await channel.ExchangeDeclareAsync(mainExchange, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
            await channel.QueueBindAsync(_queueName, mainExchange, _queueName, arguments: null);
        }

        private void DisposeChannel(IChannel channel)
        {
            if (channel == null) return;

            try
            {
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

            var channels = new List<IChannel>();
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
                    try
                    {
                        _channelSemaphore.Release();
                    }
                    catch (ObjectDisposedException ex)
                    {
                        _logger.LogWarning(ex, "Attempted to release a disposed semaphore in CleanupIdleChannels.");
                    }
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
            _queueDeclareLock.Dispose();
        }

        public string QueueName => _queueName;
    }
}
