using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Notifications
{
    public class ScheduledNotification : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public Guid FormId { get; set; }

        public Guid EmailTemplateId { get; set; }

        public Guid? ApplicationStatusId { get; set; }

        public string? ApplicationStatus { get; set; }

        public string TriggerType { get; set; } = string.Empty; // Date or Event

        public string? TriggerDetail { get; set; }

        public bool IsActive { get; set; } = true;

        // Event specific
        public string? EventType { get; set; }
        public string? RecipientCategory { get; set; }
        public string? RecipientIdentifier { get; set; }

        // Date specific
        public string? DateField { get; set; }
    }
}
