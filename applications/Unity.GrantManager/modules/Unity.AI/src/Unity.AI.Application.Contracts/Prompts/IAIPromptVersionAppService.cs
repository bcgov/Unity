using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.AI.Prompts;

public interface IAIPromptVersionAppService : ICrudAppService<
    AIPromptVersionDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateAIPromptVersionDto>
{
    System.Threading.Tasks.Task<ListResultDto<AIPromptVersionDto>> GetByPromptAsync(Guid promptId);
}
