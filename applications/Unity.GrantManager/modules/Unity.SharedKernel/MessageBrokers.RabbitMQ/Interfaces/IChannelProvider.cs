using RabbitMQ.Client;
using System;
using System.Threading.Tasks;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces
{
    public interface IChannelProvider : IDisposable
    {
        Task<IChannel?> GetChannelAsync();
        void ReturnChannel(IChannel channel);
    }
}
