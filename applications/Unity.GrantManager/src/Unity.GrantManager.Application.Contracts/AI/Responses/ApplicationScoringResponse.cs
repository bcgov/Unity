using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.GrantManager.AI
{
    public class ScoresheetSectionResponse
    {
        [JsonPropertyName("answers")]
        public Dictionary<string, ScoresheetSectionAnswer> Answers { get; set; } = new();
    }
}
