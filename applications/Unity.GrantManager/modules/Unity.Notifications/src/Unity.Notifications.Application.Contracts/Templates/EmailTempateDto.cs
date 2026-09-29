using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.Notifications.Templates
{
    [Serializable]
    public class EmailTempateDto
    {
        public Guid? TenantId { get; set; }
        // Server-side guard matching the maxlength on the template editor input
        [StringLength(EmailTemplateConsts.MaxNameLength)]
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Subject { get; set; } = "";

        public string BodyText { get; set; } = "";
        public string BodyHTML { get; set; } = "";
        public string SendFrom { get; set; } = "";
        public string? RecipientCategory { get; set; }
        public string? RecipientIdentifier { get; set; }
        public string TemplateType { get; set; } = "Application";
    }
}
