using System.Text.Json.Serialization;

namespace Unity.GrantManager.AI
{
    public class ApplicationAnalysisFinding
    {
        [JsonPropertyName(AIJsonKeys.Id)]
        public string? Id { get; set; }

        [JsonPropertyName(AIJsonKeys.Title)]
        public string? Title { get; set; }

        [JsonPropertyName(AIJsonKeys.Detail)]
        public string? Detail { get; set; }
    }
}
