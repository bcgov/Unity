using System.Text.Json.Serialization;

namespace Unity.GrantManager.AI
{
    public class AttachmentSummaryResponse
    {
        [JsonPropertyName(AIJsonKeys.Summary)]
        public string Summary { get; set; } = string.Empty;
    }
}
