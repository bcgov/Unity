using System;
using Unity.Notifications.Events;
using Unity.RabbitMQ.Interfaces;

namespace Unity.Notifications.Integrations.RabbitMQ.QueueMessages
{
    public class EmailMessages : IQueueMessage
    {
        public Guid MessageId { get; set; }
        public TimeSpan TimeToLive { get; set; }
        public required EmailNotificationEvent EmailNotificationEvent { get; set; }
    }
}
