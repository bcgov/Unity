using System;

namespace Unity.GrantManager.Intakes
{
    public class EventSubscription
    {        
        public Guid FormId { get; set; }
        public Guid FormVersion { get; set; }
        public Guid SubmissionId { get; set; }        
        public string? SubscriptionEvent { get; set; }
    }
}
