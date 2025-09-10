using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Threading;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public sealed class ChannelProvider : IChannelProvider, IDisposable
    {
        private readonly IConnectionProvider _connectionProvider;
        private readonly ILogger<ChannelProvider> _logger;
        private readonly int _maxChannels;
        private readonly ConcurrentQueue<IModel> _channelPool = new();
        private int _currentChannelCount;
        private bool _disposed;
        private const int DefaultMaxChannels = 10000;

        public ChannelProvider(IConnectionProvider connectionProvider, ILogger<ChannelProvider> logger, int maxChannels = DefaultMaxChannels)
        {
            _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxChannels = maxChannels;
        }

        public IModel? GetChannel()
        {
            ThrowIfDisposed();

            // Try to reuse a channel from the pool
            while (_channelPool.TryDequeue(out var channel))
            {
                if (channel.IsOpen)
                    return channel;

                DisposeChannel(channel);
            }

            // Try to create a new channel if we haven't reached max
            if (Interlocked.Increment(ref _currentChannelCount) <= _maxChannels)
            {
                try
                {
                    var connection = _connectionProvider.GetConnection();
                    if (connection != null && connection.IsOpen)
                        return connection.CreateModel();

                    _logger.LogWarning("RabbitMQ connection is not open.");
                    Interlocked.Decrement(ref _currentChannelCount); // failed to create
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating RabbitMQ channel.");
                    Interlocked.Decrement(ref _currentChannelCount); // failed to create
                }
            }
            else
            {
                Interlocked.Decrement(ref _currentChannelCount); // revert increment since max reached
                _logger.LogWarning("Max channel count reached ({MaxChannels}). Cannot create new channel.", _maxChannels);
            }

            return null;
        }

        public void ReturnChannel(IModel channel)
        {
            if (_disposed)
            {
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
            try
            {
                if (channel.IsOpen)
                    channel.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception while closing RabbitMQ channel.");
            }

            try
            {
                channel.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception while disposing RabbitMQ channel.");
            }

            Interlocked.Decrement(ref _currentChannelCount);
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(ChannelProvider));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            while (_channelPool.TryDequeue(out var channel))
            {
                DisposeChannel(channel);
            }
        }
    }
}
