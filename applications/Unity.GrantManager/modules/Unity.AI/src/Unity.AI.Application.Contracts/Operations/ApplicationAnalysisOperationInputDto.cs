using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unity.AI.Models;

namespace Unity.AI.Operations
{
    public class ApplicationAnalysisOperationInputDto
    {
        [JsonPropertyName("applicationId")]
        public Guid ApplicationId { get; set; }

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
