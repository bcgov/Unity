using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public sealed class PooledChannelProvider(
        IConnectionProvider connectionProvider,
        ILogger<PooledChannelProvider> logger,
        int maxChannels = PooledChannelProvider.DefaultMaxChannels) : IChannelProvider, IDisposable
    {
        private readonly IConnectionProvider _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        private readonly ILogger<PooledChannelProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly int _maxChannels = maxChannels;
        private readonly ConcurrentQueue<IModel> _channelPool = new();
        private int _currentChannelCount;
        private bool _disposed;

        private const int DefaultMaxChannels = 1000;

        /// <summary>
        /// Get a channel from the pool or create a new one if under max limit
        /// </summary>
        public IModel? GetChannel()
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
                    var connection = _connectionProvider.GetConnection();
                    if (connection != null && connection.IsOpen)
                    {
                        return connection.CreateModel();
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
        public void ReturnChannel(IModel channel)
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

        private void DisposeChannel(IModel channel)
        {
            if (channel == null) return;

            try { if (channel.IsOpen) channel.Close(); } catch (Exception ex) { _logger.LogWarning(ex, "Error closing channel."); }
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
