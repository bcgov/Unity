using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.AI.Requests;

public sealed class ApplicationScoringBatchRequest
{
    [JsonPropertyName("applicationId")]
    public Guid ApplicationId { get; set; }

    [JsonPropertyName("sections")]
    public List<ApplicationScoringBatchSectionRequest> Sections { get; set; } = [];

    [JsonPropertyName("attachments")]
    public List<ApplicationScoringBatchAttachmentRequest> Attachments { get; set; } = [];

    [JsonPropertyName("promptVersion")]
    public string? PromptVersion { get; set; }
}

public sealed class ApplicationScoringBatchSectionRequest
{
    [JsonPropertyName("sectionId")]
    public string SectionId { get; set; } = string.Empty;

    [JsonPropertyName("sectionName")]
    public string SectionName { get; set; } = string.Empty;
}

public sealed class ApplicationScoringBatchAttachmentRequest
{
    [JsonPropertyName("attachmentId")]
    public string AttachmentId { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}
