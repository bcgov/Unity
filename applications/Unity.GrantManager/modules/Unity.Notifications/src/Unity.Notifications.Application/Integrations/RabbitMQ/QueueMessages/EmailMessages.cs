using System;
using Unity.Notifications.Events;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.Notifications.Integrations.RabbitMQ.QueueMessages
{
    public class EmailMessages : ITenantedQueueMessage
    {
        public Guid MessageId { get; set; }
        public TimeSpan TimeToLive { get; set; }
        public Guid TenantId { get; set; }
        public required EmailNotificationEvent EmailNotificationEvent { get; set; }
    }
}
