using System;
using System.Collections.Generic;

namespace Unity.AI.Generation;

public sealed class AIGenerationSubmissionDto
{
    public Guid ApplicationId { get; set; }

    public Guid? ApplicationFormVersionId { get; set; }

    public List<Guid> AttachmentIds { get; set; } = [];

    public string? PromptVersion { get; set; }
}
