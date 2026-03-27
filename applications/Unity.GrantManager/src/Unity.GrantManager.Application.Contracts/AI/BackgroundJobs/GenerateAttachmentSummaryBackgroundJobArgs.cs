using System;
using System.Collections.Generic;

namespace Unity.GrantManager.AI.BackgroundJobs;

public class GenerateAttachmentSummaryBackgroundJobArgs
{
    public List<Guid> AttachmentIds { get; set; } = [];
    public string? PromptVersion { get; set; }
    public Guid? TenantId { get; set; }
}
