using System;
namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
public class RunApplicationAIPipelineJobArgs
{
    public Guid ApplicationId { get; set; }
    public Guid? TenantId { get; set; }
    public string? PromptVersion { get; set; }
    public string RequestKey { get; set; } = string.Empty;
}
