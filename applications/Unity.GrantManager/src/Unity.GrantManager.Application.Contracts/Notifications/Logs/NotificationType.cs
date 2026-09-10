using System.Text.Json.Serialization;

namespace Unity.GrantManager.Notifications.Logs
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NotificationType
    {
        DatabaseException,
        UnityException,
        UnityAlert,
        UnityNotification,
        ChefsEvent
    }
}