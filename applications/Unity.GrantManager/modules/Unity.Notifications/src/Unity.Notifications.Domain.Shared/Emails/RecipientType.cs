using System.Text.Json.Serialization;

namespace Unity.Notifications.Emails;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RecipientType
{
    Internal,
    External
}
