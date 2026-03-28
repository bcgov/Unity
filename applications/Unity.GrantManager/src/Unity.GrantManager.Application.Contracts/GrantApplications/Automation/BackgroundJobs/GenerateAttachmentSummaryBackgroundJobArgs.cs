using System;
using System.Collections.Generic;
namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
public class GenerateAttachmentSummaryBackgroundJobArgs
{
    public List<Guid> AttachmentIds { get; set; } = [];
    public Guid? TenantId { get; set; }
    public string? PromptVersion { get; set; }
}