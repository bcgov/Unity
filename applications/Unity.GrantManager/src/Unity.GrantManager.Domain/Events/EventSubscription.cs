using System;

namespace Unity.GrantManager.Events
{
    public class EventSubscription
    {        
        public Guid FormId { get; set; }
        public Guid FormVersion { get; set; }
        public Guid SubmissionId { get; set; }        
        public string? SubscriptionEvent { get; set; }
    }
}
