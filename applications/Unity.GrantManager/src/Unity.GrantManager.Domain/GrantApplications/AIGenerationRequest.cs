using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.GrantApplications;

public class AIGenerationRequest : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid? ApplicationId { get; set; }
    public Guid? AttachmentId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string? PromptVersion { get; set; }
    public string RequestKey { get; set; } = string.Empty;
    public AIGenerationRequestStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
    public bool IsActive => Status is AIGenerationRequestStatus.Queued or AIGenerationRequestStatus.Running;

    protected AIGenerationRequest()
    {
    }

    public AIGenerationRequest(
        Guid id,
        Guid? tenantId,
        string operationType,
        Guid? applicationId,
        Guid? attachmentId,
        string? promptVersion,
        string requestKey)
        : base(id)
    {
        TenantId = tenantId;
        OperationType = operationType;
        ApplicationId = applicationId;
        AttachmentId = attachmentId;
        PromptVersion = promptVersion;
        RequestKey = requestKey;
        Status = AIGenerationRequestStatus.Queued;
    }

    public void MarkRunning(DateTime startedAt)
    {
        Status = AIGenerationRequestStatus.Running;
        StartedAt = startedAt;
        FailureReason = null;
    }

    public void MarkCompleted(DateTime completedAt)
    {
        Status = AIGenerationRequestStatus.Completed;
        CompletedAt = completedAt;
        FailureReason = null;
    }

    public void MarkFailed(DateTime completedAt, string? failureReason)
    {
        Status = AIGenerationRequestStatus.Failed;
        CompletedAt = completedAt;
        FailureReason = failureReason;
    }
}
