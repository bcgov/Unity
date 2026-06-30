using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.AI.Domain;

public class AIPrompt : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; protected set; }

    public string Name { get; set; } = default!;

    public int VersionNumber { get; set; }

    public string SystemPrompt { get; set; } = default!;

    public string UserPrompt { get; set; } = default!;

    public string? MetadataJson { get; set; }

    public bool IsActive { get; set; } = true;

    protected AIPrompt() { }

    public AIPrompt(
        Guid id,
        string name,
        int versionNumber,
        string systemPrompt,
        string userPrompt,
        Guid? tenantId = null)
    {
        Id = id;
        Name = name;
        VersionNumber = versionNumber;
        SystemPrompt = systemPrompt;
        UserPrompt = userPrompt;
        TenantId = tenantId;
        IsActive = true;
    }
}
