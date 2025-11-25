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

        public SharedChannelProvider(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public IModel GetChannel()
        {
            if (_channel == null || _channel.IsClosed)
            {
                lock (_lock)
                {
                    if (_channel == null || _channel.IsClosed)
                    {
                        var connection = _connectionProvider.GetConnection();
                        if (connection != null)
                        {
                            _channel = connection.CreateModel();
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
            _channel?.Dispose();
        }
    }
}