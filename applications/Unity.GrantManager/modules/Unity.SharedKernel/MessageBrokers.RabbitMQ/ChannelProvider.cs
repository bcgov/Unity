using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public sealed class PooledChannelProvider(
        IConnectionProvider connectionProvider,
        ILogger<PooledChannelProvider> logger,
        int maxChannels = PooledChannelProvider.DefaultMaxChannels) : IChannelProvider
    {
        private readonly IConnectionProvider _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        private readonly ILogger<PooledChannelProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly int _maxChannels = maxChannels;
        private readonly ConcurrentQueue<IChannel> _channelPool = new();
        private int _currentChannelCount;
        private bool _disposed;

        private const int DefaultMaxChannels = 1000;

        /// <summary>
        /// Get a channel from the pool or create a new one if under max limit.
        /// Channels are created with publisher confirmations enabled so producers can
        /// rely on <see cref="IChannel.BasicPublishAsync"/> awaiting broker confirmation.
        /// </summary>
        public async Task<IChannel?> GetChannelAsync()
        {
            ThrowIfDisposed();

            // Try to reuse a channel
            while (_channelPool.TryDequeue(out var channel))
            {
                if (channel.IsOpen) return channel;
                DisposeChannel(channel);
            }

            // Create a new channel if we have capacity
            if (Interlocked.Increment(ref _currentChannelCount) <= _maxChannels)
            {
                try
                {
                    var connection = await _connectionProvider.GetConnectionAsync();
                    if (connection != null && connection.IsOpen)
                    {
                        return await connection.CreateChannelAsync(
                            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true));
                    }

                    _logger.LogWarning("RabbitMQ connection is not open.");
                    Interlocked.Decrement(ref _currentChannelCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating RabbitMQ channel.");
                    Interlocked.Decrement(ref _currentChannelCount);
                }
            }
            else
            {
                Interlocked.Decrement(ref _currentChannelCount);
                _logger.LogWarning("Max channel count reached ({MaxChannels}). Cannot create new channel.", _maxChannels);
            }

            return null;
        }

        /// <summary>
        /// Return a channel to the pool
        /// </summary>
        public void ReturnChannel(IChannel channel)
        {
            if (_disposed || channel == null)
            {
                if (channel != null)
                    DisposeChannel(channel);
                return;
            }

            if (channel.IsOpen)
                _channelPool.Enqueue(channel);
            else
                DisposeChannel(channel);
        }

        private void DisposeChannel(IChannel channel)
        {
            if (channel == null) return;

            try { channel.Dispose(); } catch (Exception ex) { _logger.LogWarning(ex, "Error disposing channel."); }

            Interlocked.Decrement(ref _currentChannelCount);
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(PooledChannelProvider));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            while (_channelPool.TryDequeue(out var channel))
            {
                DisposeChannel(channel);
            }
        }
    }
}
