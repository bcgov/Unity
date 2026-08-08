using System;
using System.Collections.Generic;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public sealed class AIGenerationBackgroundJobArgs
{
    public string OperationType { get; set; } = null!;

    public Guid ApplicationId { get; set; }

    public Guid OperationId { get; set; }

    public Guid? TenantId { get; set; }

    public Guid? RequestedByUserId { get; set; }

    public Guid? ApplicationFormVersionId { get; set; }

    public List<Guid> AttachmentIds { get; set; } = [];

    public string? PromptVersion { get; set; }
}
