using RabbitMQ.Client;
using System;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces
{
    public interface IChannelProvider : IDisposable
    {
        IModel? GetChannel();
        void ReturnChannel(IModel channel);
    }
}
