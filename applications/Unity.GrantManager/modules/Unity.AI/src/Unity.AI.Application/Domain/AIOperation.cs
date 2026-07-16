using System;
using Unity.AI.Operations;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.AI.Domain;

public class AIOperation : AuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = default!;

    public Guid AIModelId { get; set; }

    public AIModel AIModel { get; set; } = default!;

    public Guid AIPromptId { get; set; }

    public AIPrompt? AIPrompt { get; set; }

    public AIExecutionMode ExecutionMode { get; set; } = AIExecutionMode.Sequential;

    public int CompletionTokens { get; set; }

    public bool IsActive { get; set; } = true;

    protected AIOperation()
    {
    }

    public AIOperation(Guid id, string name, Guid aiModelId, Guid aiPromptId)
    {
        Id = id;
        Name = name;
        AIModelId = aiModelId;
        AIPromptId = aiPromptId;
    }
}
