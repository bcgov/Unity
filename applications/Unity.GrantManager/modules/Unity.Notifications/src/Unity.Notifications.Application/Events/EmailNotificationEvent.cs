using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Unity.Notifications.Emails;

namespace Unity.Notifications.Events
{
    public class EmailNotificationEvent
    {
        public Guid Id { get; set; }
        public Guid? TenantId { get; set; }
        public Guid TemplateId { get; set; }
        public Guid ApplicationId { get; set; }
        public int RetryAttempts { get; set; } = 0;
        public string Body { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? EmailFrom { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public List<string> EmailAddressList { get; set; } = [];
        public IEnumerable<string> Cc { get; set; } = [];
        public IEnumerable<string> Bcc { get; set; } = [];

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EmailAction Action { get; set; }
        public string? EmailTemplateName { get; set; } = string.Empty;
        public List<EmailAttachmentData>? EmailAttachments { get; set; }
    }

    public class EmailAttachmentData
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/octet-stream";
    }
}
