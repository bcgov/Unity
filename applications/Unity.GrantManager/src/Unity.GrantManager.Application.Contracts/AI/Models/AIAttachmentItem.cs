using System.Text.Json.Serialization;

namespace Unity.GrantManager.AI.Models
{
    public class AIAttachmentItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName(AIJsonKeys.Summary)]
        public string Summary { get; set; } = string.Empty;
    }
}

