using System;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public class GenerateFormScoresheetBackgroundJobArgs
{
    public Guid ApplicationId { get; set; }
    public Guid OperationId { get; set; }

    public Guid? TenantId { get; set; }

    public Guid? RequestedByUserId { get; set; }

    public Guid ApplicationFormVersionId { get; set; }

    public string? PromptVersion { get; set; }
}
