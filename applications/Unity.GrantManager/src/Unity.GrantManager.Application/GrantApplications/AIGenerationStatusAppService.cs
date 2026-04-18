using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Threading;

namespace Unity.GrantManager.GrantApplications;

public class AIGenerationStatusAppService(
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentTenant currentTenant)
    : ApplicationService, IAIGenerationStatusAppService
{
    public virtual async Task<AIGenerationRequestDto?> GetLatestAsync(Guid applicationId, string operationType, string? promptVersion = null, Guid? tenantId = null)
    {
        var query = await generationRequestRepository.GetQueryableAsync();
        var resolvedTenantId = tenantId ?? currentTenant.Id;

        var item = await AsyncExecuter.FirstOrDefaultAsync(
            query.Where(x =>
                    x.ApplicationId == applicationId &&
                    x.OperationType == operationType &&
                    x.TenantId == resolvedTenantId &&
                    (promptVersion == null || x.PromptVersion == promptVersion))
                .OrderByDescending(x => x.CreationTime)
                .ThenByDescending(x => x.Id));

        return item == null
            ? null
            : ObjectMapper.Map<AIGenerationRequest, AIGenerationRequestDto>(item);
    }
}
