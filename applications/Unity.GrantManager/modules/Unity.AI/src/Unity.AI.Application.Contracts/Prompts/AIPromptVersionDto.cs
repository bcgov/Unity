using System;
using Volo.Abp.Application.Dtos;

namespace Unity.AI.Prompts;

public class AIPromptVersionDto : AuditedEntityDto<Guid>
{
    public Guid PromptId { get; set; }
    public int VersionNumber { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPromptTemplate { get; set; } = string.Empty;
    public string? DeveloperNotes { get; set; }
    public string? TargetModel { get; set; }
    public string? TargetProvider { get; set; }
    public double Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public bool IsPublished { get; set; }
    public bool IsDeprecated { get; set; }
    public string? MetadataJson { get; set; }
}
