using System;
using Volo.Abp.Application.Dtos;

namespace Unity.AI.Prompts;

public class AIPromptVersionDto : AuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public bool IsActive { get; set; }
}
