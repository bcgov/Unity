using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Unity.AI.Prompts;

public class CreateUpdateAIPromptVersionDto
{
    public Guid PromptId { get; set; }

    [DisplayName("VersionNumber")]
    public int VersionNumber { get; set; }

    [Required]
    [DisplayName("SystemPrompt")]
    public string SystemPrompt { get; set; } = string.Empty;

    [Required]
    [DisplayName("UserPromptTemplate")]
    public string UserPromptTemplate { get; set; } = string.Empty;

    [DisplayName("DeveloperNotes")]
    public string? DeveloperNotes { get; set; }

    [MaxLength(100)]
    [DisplayName("TargetModel")]
    public string? TargetModel { get; set; }

    [MaxLength(100)]
    [DisplayName("TargetProvider")]
    public string? TargetProvider { get; set; }

    [DisplayName("Temperature")]
    public double Temperature { get; set; } = 0.2;

    [DisplayName("MaxTokens")]
    public int? MaxTokens { get; set; }

    [DisplayName("IsPublished")]
    public bool IsPublished { get; set; }

    [DisplayName("IsDeprecated")]
    public bool IsDeprecated { get; set; }

    [DisplayName("MetadataJson")]
    public string? MetadataJson { get; set; }
}
