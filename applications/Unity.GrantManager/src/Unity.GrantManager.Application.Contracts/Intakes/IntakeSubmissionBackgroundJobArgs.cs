using System;
using Unity.GrantManager.Events;

namespace Unity.GrantManager.Intakes
{
    public class IntakeSubmissionBackgroundJobArgs
    {
        public EventSubscriptionDto? EventSubscriptionDto { get; set; }
        public Guid? TenantId { get; set; }
        public Guid ConfirmationId { get; set; }
    }
}
