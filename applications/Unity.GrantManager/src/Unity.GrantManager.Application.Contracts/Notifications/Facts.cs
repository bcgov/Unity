using System.Text.Json.Serialization;

namespace Unity.Notifications.TeamsNotifications
{
    public class Fact
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }
}