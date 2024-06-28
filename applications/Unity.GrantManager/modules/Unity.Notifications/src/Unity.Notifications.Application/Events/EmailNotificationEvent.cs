using System;
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
        public string EmailAddress {  get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EmailAction Action { get; set; }
    }
}
