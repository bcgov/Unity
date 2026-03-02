using System.Collections.Generic;
using System.Text.Json;

namespace Unity.GrantManager.AI
{
    public class ScoresheetSectionRequest
    {
        public JsonElement Data { get; set; }
        public List<AIAttachmentItem> Attachments { get; set; } = new();
        public string SectionName { get; set; } = string.Empty;
        public JsonElement SectionSchema { get; set; }
    }
}
