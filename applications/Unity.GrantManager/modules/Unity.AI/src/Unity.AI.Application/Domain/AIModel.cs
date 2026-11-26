using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.AI.Domain;

public class AIModel : AuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = default!;

    public string Provider { get; set; } = default!;

    public bool IsActive { get; set; } = true;

    public AIModelType Type { get; set; } = AIModelType.Chat;

    public string ApiVersion { get; set; } = string.Empty;

    protected AIModel()
    {
    }

    public AIModel(Guid id, string name, string provider)
    {
        Id = id;
        Name = name;
        Provider = provider;
    }
}
