using System;

namespace Unity.GrantManager.AI.BackgroundJobs;

public class GenerateApplicationAIContentBackgroundJobArgs
{
    public Guid ApplicationId { get; set; }
    public Guid? TenantId { get; set; }
}
