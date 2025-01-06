namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces
{
    public interface IQueueProducer<in TQueueMessage> where TQueueMessage : IQueueMessage
    {
        void PublishMessage(TQueueMessage message);
    }
}
