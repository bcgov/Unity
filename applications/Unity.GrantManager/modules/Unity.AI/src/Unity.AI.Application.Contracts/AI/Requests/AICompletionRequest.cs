using System.Text.Json.Serialization;

namespace Unity.AI.Requests
{
    public class AICompletionRequest
    {
        [JsonPropertyName("userPrompt")]
        public string UserPrompt { get; set; } = string.Empty;

        [JsonPropertyName("maxTokens")]
        public int MaxTokens { get; set; } = 150;

        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }
    }
}
