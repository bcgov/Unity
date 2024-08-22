namespace Unity.Shared.MessageBrokers.RabbitMQ.Interfaces
{
    public interface IQueueConsumer<in TQueueMessage> where TQueueMessage : class, IQueueMessage
    {
        Task<Task> ConsumeAsync(TQueueMessage message);
    }
}

