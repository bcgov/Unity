using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.GrantManager.AI
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

        [JsonPropertyName(AIJsonKeys.Dismissed)]
        public List<string> Dismissed { get; set; } = new();
    }
}
