using System.Collections.Generic;
using System.Text.Json.Serialization;
using Unity.AI.Models;

namespace Unity.AI.Responses
{
    public class ApplicationScoringResponse
    {
        [JsonPropertyName("answers")]
        public Dictionary<string, ApplicationScoringAnswer> Answers { get; set; } = new();
    }
}
