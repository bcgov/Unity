using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

public class AIGenerationRequestDto : EntityDto<Guid>
{
    public Guid? ApplicationId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string RequestKey { get; set; } = string.Empty;
    public AIGenerationRequestStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
    public bool IsActive { get; set; }
}
