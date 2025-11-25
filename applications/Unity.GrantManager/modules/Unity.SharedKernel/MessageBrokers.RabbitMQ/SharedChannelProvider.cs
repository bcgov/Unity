using System;
using RabbitMQ.Client;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public class SharedChannelProvider : IChannelProvider, IDisposable
    {
        private readonly IConnectionProvider _connectionProvider;
        private IModel? _channel;
        private readonly object _lock = new object();
        private bool _disposed;

        public SharedChannelProvider(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public IModel GetChannel()
        {
            if (_channel == null || !_channel.IsOpen)
            {
                lock (_lock)
                {
                    ObjectDisposedException.ThrowIf(_disposed, nameof(SharedChannelProvider));
                    
                    if (_channel == null || !_channel.IsOpen)
                    {
                        var connection = _connectionProvider.GetConnection();
                        if (connection != null)
                        {
                            var newChannel = connection.CreateModel();
                            if (newChannel == null || !newChannel.IsOpen)
                            {
                                throw new InvalidOperationException("Unable to create an open channel.");
                            }
                            _channel = newChannel;
                        }
                        else
                        {
                            throw new InvalidOperationException("Unable to create channel: connection is not available.");
                        }
                    }
                }
            }
            return _channel;
        }

        public void ReturnChannel(IModel channel)
        {
            // For shared channel provider, we don't return channels
            // The channel is managed by this provider and disposed when the provider is disposed
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
                
                if (_channel != null)
                {
                    try
                    {
                        if (_channel.IsOpen)
                        {
                            _channel.Close();
                        }
                    }
                    catch
                    {
                        // Ignore errors during close
                    }
                    
                    try
                    {
                        _channel.Dispose();
                    }
                    catch
                    {
                        // Ignore errors during dispose
                    }
                    
                    _channel = null;
                }
            }
        }
    }
}