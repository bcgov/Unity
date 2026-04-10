using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unity.AI.Models;

namespace Unity.AI.Requests
{
    public class ApplicationAnalysisRequest
    {
        [JsonPropertyName("schema")]
        public JsonElement Schema { get; set; }

        [JsonPropertyName("data")]
        public JsonElement Data { get; set; }

        [JsonPropertyName("attachments")]
        public List<AIAttachmentItem> Attachments { get; set; } = new();

        [JsonPropertyName("promptVersion")]
        public string? PromptVersion { get; set; }
    }
}
