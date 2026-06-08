using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Notifications
{
    public class NotificationDto : EntityDto<Guid>
    {
        public Guid FormId { get; set; }
        public Guid EmailTemplateId { get; set; }
        public string? TemplateName { get; set; }
        public string TriggerType { get; set; } = string.Empty;
        public string? TriggerDetail { get; set; }
        public bool IsActive { get; set; }
        public string? EventType { get; set; }
        public Guid? ApplicationStatusId { get; set; }
        public string? ApplicationStatus { get; set; }
        public string? DateField { get; set; }
    }
}
