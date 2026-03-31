using System.Text.Json.Serialization;

namespace Unity.GrantManager.AI.Responses
{
    public class AICompletionResponse
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }
}
