using System;
using System.Collections.Generic;

namespace Unity.Notifications.Events
{
    /// <summary>
    /// Distributed event published when FSB payment notification email is successfully sent via CHES
    /// </summary>
    public class FsbEmailSentEto
    {
        public Guid EmailLogId { get; set; }
        public List<Guid> PaymentRequestIds { get; set; } = new();
        public DateTime SentDate { get; set; }
        public Guid? TenantId { get; set; }
    }
}
