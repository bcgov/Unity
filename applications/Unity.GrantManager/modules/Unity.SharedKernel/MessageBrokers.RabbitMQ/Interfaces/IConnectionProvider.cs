using RabbitMQ.Client;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces
{
    public interface IConnectionProvider
    {
        IConnection? GetConnection();
    }
}
