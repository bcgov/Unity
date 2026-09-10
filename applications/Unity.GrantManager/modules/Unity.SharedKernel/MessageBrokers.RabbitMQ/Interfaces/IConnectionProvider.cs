using RabbitMQ.Client;
using System.Threading.Tasks;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces
{
    public interface IConnectionProvider
    {
        Task<IConnection?> GetConnectionAsync();
    }
}
