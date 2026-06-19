using System.Text.Json.Serialization;

namespace Unity.Notifications.Emails;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmailType
{
    Manual,
    Scheduled,
    EventBased,
    Delayed
}
