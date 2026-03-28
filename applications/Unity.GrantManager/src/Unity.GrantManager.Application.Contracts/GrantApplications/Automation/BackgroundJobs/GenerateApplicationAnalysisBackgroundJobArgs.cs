using System;

namespace Unity.GrantManager.AI.BackgroundJobs;

public class GenerateApplicationAnalysisBackgroundJobArgs
{
    public Guid ApplicationId { get; set; }
    public string? PromptVersion { get; set; }
    public Guid? TenantId { get; set; }
}
