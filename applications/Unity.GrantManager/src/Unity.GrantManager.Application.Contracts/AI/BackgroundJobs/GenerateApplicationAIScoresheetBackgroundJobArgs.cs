using System;

namespace Unity.GrantManager.AI.BackgroundJobs;

public class GenerateApplicationAIScoresheetBackgroundJobArgs
{
    public Guid ApplicationId { get; set; }
    public string? PromptVersion { get; set; }
    public bool CapturePromptIo { get; set; }
    public Guid? TenantId { get; set; }
}
