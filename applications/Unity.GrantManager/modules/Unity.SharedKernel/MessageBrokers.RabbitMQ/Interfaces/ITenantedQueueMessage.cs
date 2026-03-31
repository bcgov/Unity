using System;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces
{
    /// <summary>
    /// Extends <see cref="IQueueMessage"/> for messages that carry tenant context.
    /// Implementing this interface causes <see cref="QueueConsumerHandler{TMessageConsumer,TQueueMessage}"/>
    /// to automatically establish background-job auditing scope before invoking the consumer,
    /// mirroring the way ASP.NET Core middleware wraps controller actions.
    /// </summary>
    public interface ITenantedQueueMessage : IQueueMessage
    {
        Guid TenantId { get; set; }
    }
}
