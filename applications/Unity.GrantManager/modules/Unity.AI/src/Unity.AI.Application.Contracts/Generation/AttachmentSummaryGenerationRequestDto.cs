using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Unity.AI.Generation;

public class AttachmentSummaryGenerationRequestDto
{
    [Required]
    [JsonPropertyName("applicationId")]
    public Guid ApplicationId { get; set; }

    [Required]
    [JsonPropertyName("attachmentIds")]
    public List<Guid> AttachmentIds { get; set; } = [];

    [JsonPropertyName("promptVersion")]
    public string? PromptVersion { get; set; }
}
