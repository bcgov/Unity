namespace Unity.Shared.MessageBrokers.RabbitMQ.Interfaces
{
    /// <summary>
    /// A channel provider that Declares and Binds a specific queue
    /// </summary>
#pragma warning disable S2326
    public interface IQueueChannelProvider<in TQueueMessage> : IChannelProvider where TQueueMessage : IQueueMessage
    {
    }
#pragma warning restore S2326
}

