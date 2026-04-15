using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Threading;

namespace Unity.GrantManager.GrantApplications;

public class AIGenerationStatusAppService(
    IRepository<AIGenerationRequest, Guid> generationRequestRepository)
    : ApplicationService, IAIGenerationStatusAppService
{
    public virtual async Task<AIGenerationRequestDto?> GetLatestAsync(Guid applicationId, string operationType, string? promptVersion = null)
    {
        var query = await generationRequestRepository.GetQueryableAsync();

        var item = await AsyncExecuter.FirstOrDefaultAsync(
            query.Where(x =>
                    x.ApplicationId == applicationId &&
                    x.OperationType == operationType &&
                    (promptVersion == null || x.PromptVersion == promptVersion))
                .OrderByDescending(x => x.CreationTime)
                .ThenByDescending(x => x.Id));

        return item == null
            ? null
            : ObjectMapper.Map<AIGenerationRequest, AIGenerationRequestDto>(item);
    }
}
