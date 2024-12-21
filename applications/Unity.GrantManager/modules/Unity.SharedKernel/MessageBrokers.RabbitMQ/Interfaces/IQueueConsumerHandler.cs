namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces
{
#pragma warning disable S2326
    public interface IQueueConsumerHandler<TMessageConsumer, TQueueMessage> where TMessageConsumer : IQueueConsumer<TQueueMessage> where TQueueMessage : class, IQueueMessage
    {
        void RegisterQueueConsumer();

        void CancelQueueConsumer();
    }
#pragma warning restore S2326
}