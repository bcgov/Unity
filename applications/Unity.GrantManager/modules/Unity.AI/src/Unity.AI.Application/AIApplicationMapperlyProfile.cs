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

    [MapperIgnoreTarget(nameof(AIPrompt.TenantId))]
    [MapperIgnoreTarget(nameof(AIPrompt.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(AIPrompt.CreationTime))]
    [MapperIgnoreTarget(nameof(AIPrompt.CreatorId))]
    [MapperIgnoreTarget(nameof(AIPrompt.LastModificationTime))]
    [MapperIgnoreTarget(nameof(AIPrompt.LastModifierId))]
    public override partial AIPrompt Map(CreateUpdateAIPromptDto source);

    [MapperIgnoreTarget(nameof(AIPrompt.TenantId))]
    [MapperIgnoreTarget(nameof(AIPrompt.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(AIPrompt.CreationTime))]
    [MapperIgnoreTarget(nameof(AIPrompt.CreatorId))]
    [MapperIgnoreTarget(nameof(AIPrompt.LastModificationTime))]
    [MapperIgnoreTarget(nameof(AIPrompt.LastModifierId))]
    public override partial void Map(CreateUpdateAIPromptDto source, AIPrompt destination);
}

[Mapper]
public partial class AIPromptToAIPromptVersionDtoMapper : MapperBase<AIPrompt, AIPromptVersionDto>
{
    public override partial AIPromptVersionDto Map(AIPrompt source);

    public override partial void Map(AIPrompt source, AIPromptVersionDto destination);
}
