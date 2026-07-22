using System;

namespace Unity.AI.Generation;

public class AIGenerationStatusRequestDto
{
    public Guid Id { get; set; }

    public Guid? ApplicationId { get; set; }

    public Guid? OperationId { get; set; }

    public string OperationType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? FailureReason { get; set; }

    public bool IsActive { get; set; }
}
