using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.AI.Domain;

public class AIModel : AuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = default!;

    public bool IsActive { get; set; } = true;

    /// <summary>Free-form model settings stored as JSON for dynamic runtime options.</summary>
    public string SettingsJson { get; set; } = "{}";

    protected AIModel()
    {
    }

    public AIModel(Guid id, string name)
    {
        Id = id;
        Name = name;
    }
}
