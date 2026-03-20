using System.Text.Json.Serialization;

namespace Unity.GrantManager.AI.Responses
{
    public class AttachmentSummaryResponse
    {
        [JsonPropertyName(AIJsonKeys.Summary)]
        public string Summary { get; set; } = string.Empty;
    }
}

