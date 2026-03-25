using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Unity.AI.Prompts;

public class AIPromptDto : AuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PromptType Type { get; set; }
    public bool IsActive { get; set; }
    public List<AIPromptVersionDto> Versions { get; set; } = new();
}
