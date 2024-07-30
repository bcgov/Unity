using RabbitMQ.Client;

namespace Unity.Shared.MessageBrokers.RabbitMQ.Interfaces
{
    public interface IConnectionProvider
    {
        IConnection? GetConnection();
    }
}
