using System.Collections.Generic;
using System.Text.Json.Serialization;
using Unity.AI.Models;

namespace Unity.AI.Responses
{
    public class ApplicationAnalysisResponse
    {
        [JsonPropertyName(AIJsonKeys.Decision)]
        public string? Decision { get; set; }

        [JsonPropertyName(AIJsonKeys.Errors)]
        public List<ApplicationAnalysisFinding> Errors { get; set; } = new();

        [JsonPropertyName(AIJsonKeys.Warnings)]
        public List<ApplicationAnalysisFinding> Warnings { get; set; } = new();

        [JsonPropertyName(AIJsonKeys.Summaries)]
        public List<ApplicationAnalysisFinding> Summaries { get; set; } = new();

        [JsonPropertyName(AIJsonKeys.Recommendations)]
        public List<ApplicationAnalysisFinding> Recommendations { get; set; } = new();
    }
}
