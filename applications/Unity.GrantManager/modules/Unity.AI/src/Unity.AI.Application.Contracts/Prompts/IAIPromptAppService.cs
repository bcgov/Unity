using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.AI.Prompts;

public interface IAIPromptAppService : ICrudAppService<
    AIPromptDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateAIPromptDto>
{
}
