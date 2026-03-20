using System.Collections.Generic;
using System.Text.Json.Serialization;
using Unity.GrantManager.AI.Models;

namespace Unity.GrantManager.AI.Responses
{
    public class ApplicationScoringResponse
    {
        [JsonPropertyName("answers")]
        public Dictionary<string, ApplicationScoringAnswer> Answers { get; set; } = new();
    }
}
