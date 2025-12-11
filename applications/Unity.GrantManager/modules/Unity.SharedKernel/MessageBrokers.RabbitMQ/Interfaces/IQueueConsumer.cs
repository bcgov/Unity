using System.Threading.Tasks;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces
{
    public interface IQueueConsumer<in TQueueMessage> where TQueueMessage : class, IQueueMessage
    {
        Task ConsumeAsync(TQueueMessage message);
    }
}

