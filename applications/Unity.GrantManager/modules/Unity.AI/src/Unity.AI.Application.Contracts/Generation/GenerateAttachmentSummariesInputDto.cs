using System;
using System.Collections.Generic;

namespace Unity.AI.Generation;

public class GenerateAttachmentSummariesInputDto
{
    public Guid ApplicationId { get; set; }

    public List<Guid> AttachmentIds { get; set; } = [];

    public string? PromptVersion { get; set; }
}
