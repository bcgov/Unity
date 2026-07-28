using System.Threading.Tasks;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces
{
    public interface IQueueProducer<in TQueueMessage> where TQueueMessage : IQueueMessage
    {
        Task PublishMessageAsync(TQueueMessage message);
    }
}
