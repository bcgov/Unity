using System;

namespace Unity.Notifications.Templates
{
    [Serializable]
    public class EmailTempateDto
    {
        public Guid? TenantId { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Subject { get; set; } = "";

        public string BodyText { get; set; } = "";
        public string BodyHTML { get; set; } = "";
        public string SendFrom { get; set; } = "";
    }
}
