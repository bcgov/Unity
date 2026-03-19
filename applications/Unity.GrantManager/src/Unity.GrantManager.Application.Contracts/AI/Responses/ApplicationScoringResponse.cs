using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.GrantManager.AI
{
    public class ApplicationScoringResponse
    {
        [JsonPropertyName("answers")]
        public Dictionary<string, ApplicationScoringAnswer> Answers { get; set; } = new();
    }
}

