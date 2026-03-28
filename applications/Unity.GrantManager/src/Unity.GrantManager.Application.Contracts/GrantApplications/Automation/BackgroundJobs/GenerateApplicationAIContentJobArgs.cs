using System;
namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
public class GenerateApplicationAIContentJobArgs
{
    public Guid ApplicationId { get; set; }
    public Guid? TenantId { get; set; }
    public string? PromptVersion { get; set; }
}