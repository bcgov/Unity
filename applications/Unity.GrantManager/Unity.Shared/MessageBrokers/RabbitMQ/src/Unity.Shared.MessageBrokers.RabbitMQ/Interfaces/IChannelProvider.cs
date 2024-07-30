using RabbitMQ.Client;

namespace Unity.Shared.MessageBrokers.RabbitMQ.Interfaces
{
    public interface IChannelProvider : IDisposable
    {
        IModel? GetChannel();
    }
}
