using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Threading;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public sealed class ChannelProvider(
        IConnectionProvider connectionProvider,
        ILogger<ChannelProvider> logger,
        int maxChannels = 10000) : IChannelProvider
    {
        private readonly IConnectionProvider _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        private readonly ILogger<ChannelProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ConcurrentBag<IModel> _channelPool = [];
        private int _currentChannelCount;

        private bool _disposed;

        public IModel? GetChannel()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_channelPool.TryTake(out var channel))
            {
                if (channel.IsOpen)
                {
                    return channel;
                }

                DisposeChannel(channel);
            }

            try
            {
                if (Interlocked.Increment(ref _currentChannelCount) <= maxChannels)
                {
                    var connection = _connectionProvider.GetConnection();

                    if (connection != null && connection.IsOpen)
                    {
                        return connection.CreateModel();
                    }

                    _logger.LogWarning("RabbitMQ connection is not open.");
                }
                else
                {
                    _logger.LogWarning("Max channel count reached ({MaxChannels}). Cannot create new channel.", maxChannels);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating RabbitMQ channel.");
            }
            finally
            {
                Interlocked.Decrement(ref _currentChannelCount);
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

            if (channel != null && channel.IsOpen)
            {
                _channelPool.Add(channel);
            }
            else if (channel != null)
            {
                DisposeChannel(channel);
            }
        }

        private void DisposeChannel(IModel channel)
        {
            try
            {
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

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            while (_channelPool.TryTake(out var channel))
            {
                DisposeChannel(channel);
            }
        }
    }
}
