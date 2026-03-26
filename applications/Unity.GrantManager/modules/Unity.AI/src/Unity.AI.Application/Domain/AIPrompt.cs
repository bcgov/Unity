using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.AI.Domain;

public class AIPrompt : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; protected set; }

    public string Name { get; set; } = default!;

    public string? Description { get; set; }

    public PromptType Type { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<AIPromptVersion> Versions { get; set; } = new List<AIPromptVersion>();

    protected AIPrompt() { }

    public AIPrompt(Guid id, string name, PromptType type, Guid? tenantId = null)
    {
        Id = id;
        Name = name;
        Type = type;
        TenantId = tenantId;
        IsActive = true;
    }
}
