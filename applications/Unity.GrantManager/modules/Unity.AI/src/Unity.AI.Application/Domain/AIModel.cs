using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.AI.Domain;

public class AIModel : AuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = default!;

    public string Provider { get; set; } = default!;

    public bool IsActive { get; set; } = true;

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
