using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unity.AI.Models;

namespace Unity.AI.Operations
{
    public class ApplicationScoringOperationInputDto
    {
        [JsonPropertyName("applicationId")]
        public Guid ApplicationId { get; set; }

        [JsonPropertyName("data")]
        public JsonElement Data { get; set; }

        [JsonPropertyName("attachments")]
        public List<AIAttachmentItem> Attachments { get; set; } = new();

        [JsonPropertyName("sections")]
        public List<ApplicationScoringSectionOperationInputDto> Sections { get; set; } = new();

        [JsonPropertyName("promptVersion")]
        public string? PromptVersion { get; set; }
    }

    public class ApplicationScoringSectionOperationInputDto
    {
        [JsonPropertyName("sectionName")]
        public string SectionName { get; set; } = string.Empty;

        [JsonPropertyName("sectionSchema")]
        public JsonElement SectionSchema { get; set; }
    }
}
