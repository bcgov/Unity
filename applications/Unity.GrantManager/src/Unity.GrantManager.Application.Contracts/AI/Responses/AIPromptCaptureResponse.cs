using System;
using System.Text.Json.Serialization;

namespace Unity.GrantManager.AI
{
    public class AIPromptCaptureResponse
    {
        [JsonPropertyName("contextId")]
        public string ContextId { get; set; } = string.Empty;

        [JsonPropertyName("promptType")]
        public string PromptType { get; set; } = string.Empty;

        [JsonPropertyName("promptVersion")]
        public string PromptVersion { get; set; } = string.Empty;

        [JsonPropertyName("captureLabel")]
        public string CaptureLabel { get; set; } = string.Empty;

        [JsonPropertyName("systemPrompt")]
        public string SystemPrompt { get; set; } = string.Empty;

        [JsonPropertyName("userPrompt")]
        public string UserPrompt { get; set; } = string.Empty;

        [JsonPropertyName("output")]
        public string Output { get; set; } = string.Empty;

        [JsonPropertyName("capturedAt")]
        public DateTime CapturedAt { get; set; }
    }
}
