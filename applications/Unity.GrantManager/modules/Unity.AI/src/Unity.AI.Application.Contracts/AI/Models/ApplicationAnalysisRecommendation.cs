using System.Text.Json.Serialization;

namespace Unity.AI.Models
{
    public class ApplicationAnalysisRecommendation
    {
        [JsonPropertyName(AIJsonKeys.Decision)]
        public string? Decision { get; set; }

        [JsonPropertyName(AIJsonKeys.Rationale)]
        public string? Rationale { get; set; }
    }
}
