using RabbitMQ.Client;

namespace Unity.RabbitMQ.Interfaces
{
    public interface IConnectionProvider
    {
        IConnection? GetConnection();
    }
}
