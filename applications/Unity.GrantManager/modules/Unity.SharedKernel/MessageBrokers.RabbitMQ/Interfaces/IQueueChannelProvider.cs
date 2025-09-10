using System;
using RabbitMQ.Client;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces
{
    /// <summary>
    /// Provides a RabbitMQ channel that declares and binds a specific queue and its dead-letter queue.
    /// </summary>
    public interface IQueueChannelProvider<TQueueMessage> : IDisposable where TQueueMessage : IQueueMessage
    {
        /// <summary>
        /// Gets a channel for publishing or consuming messages.
        /// </summary>
        IModel GetChannel();
    }
}
