using System.Collections.Generic;
using System.Text.Json.Serialization;
using Unity.GrantManager.AI.Models;

namespace Unity.GrantManager.AI.Responses
{
    public class ApplicationAnalysisResponse
    {
        [JsonPropertyName(AIJsonKeys.Rating)]
        public string? Rating { get; set; }

        [JsonPropertyName(AIJsonKeys.Errors)]
        public List<ApplicationAnalysisFinding> Errors { get; set; } = new();

        [JsonPropertyName(AIJsonKeys.Warnings)]
        public List<ApplicationAnalysisFinding> Warnings { get; set; } = new();

        [JsonPropertyName(AIJsonKeys.Summaries)]
        public List<ApplicationAnalysisFinding> Summaries { get; set; } = new();

        [JsonPropertyName(AIJsonKeys.NextSteps)]
        public List<ApplicationAnalysisFinding> NextSteps { get; set; } = new();

        [JsonPropertyName(AIJsonKeys.Recommendation)]
        public ApplicationAnalysisRecommendation? Recommendation { get; set; }
    }
}
