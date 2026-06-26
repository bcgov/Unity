using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Unity.AI.Prompts;

public class CreateUpdateAIPromptDto
{
    [Required]
    [MaxLength(200)]
    [DisplayName("PromptName")]
    public string Name { get; set; } = string.Empty;

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
