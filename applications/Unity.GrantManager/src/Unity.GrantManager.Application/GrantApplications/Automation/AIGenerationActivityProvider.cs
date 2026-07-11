using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Cooldown;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;

namespace Unity.GrantManager.GrantApplications.Automation;

public class AIGenerationActivityProvider(
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentUser currentUser,
    ICurrentTenant currentTenant,
    IAsyncQueryableExecuter asyncExecuter) : ITransientDependency
{
    public async Task<bool> HasActiveGenerationAsync()
    {
        if (currentUser.Id is not Guid userId)
        {
            return false;
        }

        var query = await generationRequestRepository.GetQueryableAsync();
        return await asyncExecuter.AnyAsync(query.Where(x =>
            x.CreatorId == userId &&
            x.TenantId == currentTenant.Id &&
            (x.Status == AIGenerationRequestStatus.Queued || x.Status == AIGenerationRequestStatus.Running)));
    }
}
