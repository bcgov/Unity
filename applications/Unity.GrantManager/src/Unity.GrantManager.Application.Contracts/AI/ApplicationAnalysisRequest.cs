using System.Collections.Generic;
using System.Text.Json;

namespace Unity.GrantManager.AI
{
    public class ApplicationAnalysisRequest
    {
        public JsonElement Schema { get; set; }
        public JsonElement Data { get; set; }
        public List<ApplicationAnalysisAttachment> Attachments { get; set; } = new();
        public string? Rubric { get; set; }
    }

    public class ApplicationAnalysisAttachment
    {
        public string Name { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
    }
}
