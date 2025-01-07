using System;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces
{ 
    public interface IQueueMessage
    {
        Guid MessageId { get; set; }
        TimeSpan TimeToLive { get; set; }
    }
}
