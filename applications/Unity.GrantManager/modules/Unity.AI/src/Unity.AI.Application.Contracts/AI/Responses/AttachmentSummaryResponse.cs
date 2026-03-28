using System.Text.Json.Serialization;
using Unity.AI.Models;

namespace Unity.AI.Responses
{
    public class AttachmentSummaryResponse
    {
        [JsonPropertyName(AIJsonKeys.Summary)]
        public string Summary { get; set; } = string.Empty;
    }
}
