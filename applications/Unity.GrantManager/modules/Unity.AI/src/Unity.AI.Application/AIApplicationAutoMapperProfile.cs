using AutoMapper;
using Unity.AI.Domain;
using Unity.AI.Prompts;

namespace Unity.AI;

public class AIApplicationAutoMapperProfile : Profile
{
    public AIApplicationAutoMapperProfile()
    {
        CreateMap<AIPrompt, AIPromptDto>();
        CreateMap<CreateUpdateAIPromptDto, AIPrompt>(MemberList.None);

        CreateMap<AIPromptVersion, AIPromptVersionDto>();
        CreateMap<CreateUpdateAIPromptVersionDto, AIPromptVersion>(MemberList.None);
    }
}
