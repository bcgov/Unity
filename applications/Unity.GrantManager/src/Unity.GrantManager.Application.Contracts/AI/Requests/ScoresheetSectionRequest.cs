using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Unity.GrantManager.AI
{
    public class ScoresheetSectionRequest
    {
        [JsonPropertyName("data")]
        public JsonElement Data { get; set; }

        [JsonPropertyName("attachments")]
        public List<AIAttachmentItem> Attachments { get; set; } = new();

        [JsonPropertyName("sectionName")]
        public string SectionName { get; set; } = string.Empty;

        [JsonPropertyName("sectionSchema")]
        public JsonElement SectionSchema { get; set; }
    }
}
