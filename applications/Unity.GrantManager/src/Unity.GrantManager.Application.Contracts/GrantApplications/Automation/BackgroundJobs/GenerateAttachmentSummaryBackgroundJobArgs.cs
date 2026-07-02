using System;
using System.Collections.Generic;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public class GenerateAttachmentSummaryBackgroundJobArgs
{
    public Guid ApplicationId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public List<Guid>? AttachmentIds { get; set; }
    public string? PromptVersion { get; set; }
}
