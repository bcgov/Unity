using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Unity.AI.Prompts;

public class CreateUpdateAIPromptDto
{
    public Guid PromptId { get; set; }

    [DisplayName("VersionNumber")]
    public int VersionNumber { get; set; }

    [Required]
    [DisplayName("SystemPrompt")]
    public string SystemPrompt { get; set; } = string.Empty;

    [Required]
    [DisplayName("UserPrompt")]
    public string UserPrompt { get; set; } = string.Empty;

    [DisplayName("MetadataJson")]
    public string? MetadataJson { get; set; }

    [DisplayName("PromptIsActive")]
    public bool IsActive { get; set; } = true;
}
