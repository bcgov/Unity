using System;

namespace Unity.AI.Generation;

public class AIGenerationRequestDto
{
    public Guid ApplicationId { get; set; }

    public Guid OperationId { get; set; }

    public string OperationType { get; set; } = string.Empty;
}
