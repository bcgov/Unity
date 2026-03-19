using System;

namespace Unity.GrantManager.AI.BackgroundJobs;

public class GenerateApplicationScoringBackgroundJobArgs
{
    public Guid ApplicationId { get; set; }
    public string? PromptVersion { get; set; }
    public Guid? TenantId { get; set; }
}
