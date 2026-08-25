using System;
using System.Threading.Tasks;
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
        Task<IChannel> GetChannelAsync();

        /// <summary>
        /// Returns a channel obtained from <see cref="GetChannelAsync"/> so it can be pooled
        /// or disposed and its throttling permit released. Callers that finish with a channel
        /// (for example a one-off publish) must return it; long-lived consumer channels are
        /// kept open and are not returned until the consumer is torn down.
        /// </summary>
        void ReturnChannel(IChannel channel);
    }
}
#pragma warning restore CA1005 // Avoid excessive parameters on generic types
#pragma warning restore S2326
