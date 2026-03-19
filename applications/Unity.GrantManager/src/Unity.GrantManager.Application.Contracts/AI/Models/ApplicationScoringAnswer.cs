using System.Text.Json;
using System.Text.Json.Serialization;

namespace Unity.GrantManager.AI
{
    public class ScoresheetSectionAnswer
    {
        [JsonPropertyName(AIJsonKeys.Answer)]
        public JsonElement Answer { get; set; }

        [JsonPropertyName(AIJsonKeys.Rationale)]
        public string Rationale { get; set; } = string.Empty;

        [JsonPropertyName(AIJsonKeys.Confidence)]
        public int Confidence { get; set; }
    }
}
