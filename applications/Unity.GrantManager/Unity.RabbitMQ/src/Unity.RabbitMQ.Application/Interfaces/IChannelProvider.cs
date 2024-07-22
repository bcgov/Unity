using RabbitMQ.Client;

namespace Unity.RabbitMQ.Interfaces
{
    public interface IChannelProvider : IDisposable
    {
        IModel? GetChannel();
    }
}
