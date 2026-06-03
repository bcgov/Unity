using System.Runtime.CompilerServices;
using Riok.Mapperly.Abstractions;
using Unity.AI.Domain;
using Unity.AI.Prompts;
using Volo.Abp.Mapperly;

namespace Unity.AI;

[Mapper]
public partial class AIPromptToAIPromptDtoMapper : MapperBase<AIPrompt, AIPromptDto>
{
    public override partial AIPromptDto Map(AIPrompt source);

    public override partial void Map(AIPrompt source, AIPromptDto destination);
}

[Mapper]
public partial class CreateUpdateAIPromptDtoToAIPromptMapper : MapperBase<CreateUpdateAIPromptDto, AIPrompt>
{
    [ObjectFactory]
    private static AIPrompt CreateAIPrompt() =>
        (AIPrompt)RuntimeHelpers.GetUninitializedObject(typeof(AIPrompt));

    [MapperIgnoreTarget(nameof(AIPrompt.Versions))]
    [MapperIgnoreTarget(nameof(AIPrompt.TenantId))]
    [MapperIgnoreTarget(nameof(AIPrompt.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(AIPrompt.CreationTime))]
    [MapperIgnoreTarget(nameof(AIPrompt.CreatorId))]
    [MapperIgnoreTarget(nameof(AIPrompt.LastModificationTime))]
    [MapperIgnoreTarget(nameof(AIPrompt.LastModifierId))]
    public override partial AIPrompt Map(CreateUpdateAIPromptDto source);

    [MapperIgnoreTarget(nameof(AIPrompt.Versions))]
    [MapperIgnoreTarget(nameof(AIPrompt.TenantId))]
    [MapperIgnoreTarget(nameof(AIPrompt.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(AIPrompt.CreationTime))]
    [MapperIgnoreTarget(nameof(AIPrompt.CreatorId))]
    [MapperIgnoreTarget(nameof(AIPrompt.LastModificationTime))]
    [MapperIgnoreTarget(nameof(AIPrompt.LastModifierId))]
    public override partial void Map(CreateUpdateAIPromptDto source, AIPrompt destination);
}

[Mapper]
public partial class AIPromptVersionToAIPromptVersionDtoMapper : MapperBase<AIPromptVersion, AIPromptVersionDto>
{
    public override partial AIPromptVersionDto Map(AIPromptVersion source);

    public override partial void Map(AIPromptVersion source, AIPromptVersionDto destination);
}

[Mapper]
public partial class CreateUpdateAIPromptVersionDtoToAIPromptVersionMapper : MapperBase<CreateUpdateAIPromptVersionDto, AIPromptVersion>
{
    [ObjectFactory]
    private static AIPromptVersion CreateAIPromptVersion() =>
        (AIPromptVersion)RuntimeHelpers.GetUninitializedObject(typeof(AIPromptVersion));

    [MapperIgnoreTarget(nameof(AIPromptVersion.Prompt))]
    [MapperIgnoreTarget(nameof(AIPromptVersion.TenantId))]
    [MapperIgnoreTarget(nameof(AIPromptVersion.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(AIPromptVersion.CreationTime))]
    [MapperIgnoreTarget(nameof(AIPromptVersion.CreatorId))]
    [MapperIgnoreTarget(nameof(AIPromptVersion.LastModificationTime))]
    [MapperIgnoreTarget(nameof(AIPromptVersion.LastModifierId))]
    public override partial AIPromptVersion Map(CreateUpdateAIPromptVersionDto source);

    [MapperIgnoreTarget(nameof(AIPromptVersion.Prompt))]
    [MapperIgnoreTarget(nameof(AIPromptVersion.TenantId))]
    [MapperIgnoreTarget(nameof(AIPromptVersion.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(AIPromptVersion.CreationTime))]
    [MapperIgnoreTarget(nameof(AIPromptVersion.CreatorId))]
    [MapperIgnoreTarget(nameof(AIPromptVersion.LastModificationTime))]
    [MapperIgnoreTarget(nameof(AIPromptVersion.LastModifierId))]
    public override partial void Map(CreateUpdateAIPromptVersionDto source, AIPromptVersion destination);
}
