using System;
using RabbitMQ.Client;

#pragma warning disable CA1005 // Avoid excessive parameters on generic types
#pragma warning disable S2326
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
#pragma warning restore CA1005 // Avoid excessive parameters on generic types
#pragma warning restore S2326
