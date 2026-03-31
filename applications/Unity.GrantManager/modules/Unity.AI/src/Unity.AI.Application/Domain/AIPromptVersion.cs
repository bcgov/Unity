using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.AI.Domain;

public class AIPromptVersion : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; protected set; }

    public Guid PromptId { get; set; }
    public AIPrompt? Prompt { get; set; }

    public int VersionNumber { get; set; }

    public string SystemPrompt { get; set; } = default!;
    public string UserPromptTemplate { get; set; } = default!;
    public string? DeveloperNotes { get; set; }

    public string? TargetModel { get; set; }
    public string? TargetProvider { get; set; }

    public double Temperature { get; set; } = 0.2;
    public int? MaxTokens { get; set; }

    public bool IsPublished { get; set; }
    public bool IsDeprecated { get; set; }

    /// <summary>Optional JSON metadata for extensibility (stored as Postgres jsonb).</summary>
    public string? MetadataJson { get; set; }

    protected AIPromptVersion() { }

    public AIPromptVersion(
        Guid id,
        Guid promptId,
        int versionNumber,
        string systemPrompt,
        string userPromptTemplate,
        Guid? tenantId = null)
    {
        Id = id;
        PromptId = promptId;
        VersionNumber = versionNumber;
        SystemPrompt = systemPrompt;
        UserPromptTemplate = userPromptTemplate;
        TenantId = tenantId;
        Temperature = 0.2;
    }
}
