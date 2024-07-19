namespace Unity.RabbitMQ.Interfaces
{
    /// <summary>
    /// A channel provider that Declares and Binds a specific queue
    /// </summary>
    public interface IQueueChannelProvider<in TQueueMessage> : IChannelProvider where TQueueMessage : IQueueMessage
    {
    }
}
