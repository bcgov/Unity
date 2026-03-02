using System.Text.Json.Serialization;

namespace Unity.GrantManager.AI
{
    public class AICompletionRequest
    {
        [JsonPropertyName("userPrompt")]
        public string UserPrompt { get; set; } = string.Empty;

        [JsonPropertyName("systemPrompt")]
        public string? SystemPrompt { get; set; }

        [JsonPropertyName("maxTokens")]
        public int MaxTokens { get; set; } = 150;

        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }
    }
}
