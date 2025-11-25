using System;
using RabbitMQ.Client;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public class SharedQueueChannelProvider<TMessage> : IQueueChannelProvider<TMessage>
        where TMessage : class, IQueueMessage
    {
        private readonly IChannelProvider _channelProvider;

        public SharedQueueChannelProvider(IChannelProvider channelProvider)
        {
            _channelProvider = channelProvider;
        }

        public IModel GetChannel()
        {
            return _channelProvider.GetChannel() ?? throw new InvalidOperationException("Channel provider returned null.");
        }

        public void Dispose()
        {
            // Channel is managed by SharedChannelProvider, so we don't dispose it here
        }
    }
}