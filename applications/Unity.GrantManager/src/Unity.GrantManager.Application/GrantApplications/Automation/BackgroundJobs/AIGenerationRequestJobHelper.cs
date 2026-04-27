using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public static class AIGenerationRequestJobHelper
{
    public static async Task MarkRunningAsync(
        IRepository<AIGenerationRequest, Guid> generationRequestRepository,
        AIGenerationRequest? request)
    {
        if (request == null)
        {
            return;
        }

        request.MarkRunning(DateTime.UtcNow);
        await generationRequestRepository.UpdateAsync(request, autoSave: true);
    }

    public static async Task MarkCompletedAsync(
        IRepository<AIGenerationRequest, Guid> generationRequestRepository,
        AIGenerationRequest? request)
    {
        if (request == null)
        {
            return;
        }

        request.MarkCompleted(DateTime.UtcNow);
        await generationRequestRepository.UpdateAsync(request, autoSave: true);
    }

    public static async Task MarkFailedAsync(
        IRepository<AIGenerationRequest, Guid> generationRequestRepository,
        AIGenerationRequest? request,
        string? failureReason)
    {
        if (request == null)
        {
            return;
        }

        request.MarkFailed(DateTime.UtcNow, failureReason);
        await generationRequestRepository.UpdateAsync(request, autoSave: true);
    }

    public static async Task<AIGenerationRequest?> GetLatestRequestAsync(
        IRepository<AIGenerationRequest, Guid> generationRequestRepository,
        Expression<Func<AIGenerationRequest, bool>> predicate)
    {
        var query = await generationRequestRepository.GetQueryableAsync();
        return query
            .Where(predicate)
            .OrderByDescending(x => x.CreationTime)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();
    }
}
