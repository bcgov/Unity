using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Notifications
{
    public class CreateUpdateNotificationDto
    {
        [Required]
        public Guid FormId { get; set; }

        [Required]
        public Guid EmailTemplateId { get; set; }

        [Required]
        public string TriggerType { get; set; } = "Event";

        public string? TriggerDetail { get; set; }
        public bool IsActive { get; set; } = true;

        public string? EventType { get; set; }
        public Guid? ApplicationStatusId { get; set; }

        public string? ApplicationStatus { get; set; }

        public string? DateField { get; set; }

        public string? RecipientCategory { get; set; }

        public string? RecipientIdentifier { get; set; }
    }
}
