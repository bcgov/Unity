using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

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

    public static async Task MarkRunningInNewUowAsync(
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<AIGenerationRequest, Guid> generationRequestRepository,
        string requestKey)
    {
        using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
        var request = await GetLatestRequestAsync(generationRequestRepository, x => x.RequestKey == requestKey);
        await MarkRunningAsync(generationRequestRepository, request);
        await uow.CompleteAsync();
    }

    public static async Task<Guid?> MarkCompletedInNewUowAndGetCreatorIdAsync(
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<AIGenerationRequest, Guid> generationRequestRepository,
        string requestKey)
    {
        using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
        var request = await GetLatestRequestAsync(generationRequestRepository, x => x.RequestKey == requestKey);
        await MarkCompletedAsync(generationRequestRepository, request);
        await uow.CompleteAsync();
        return request?.CreatorId;
    }

    public static async Task MarkFailedInNewUowAsync(
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<AIGenerationRequest, Guid> generationRequestRepository,
        string requestKey,
        string? failureReason)
    {
        using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
        var request = await GetLatestRequestAsync(generationRequestRepository, x => x.RequestKey == requestKey);
        await MarkFailedAsync(generationRequestRepository, request, failureReason);
        await uow.CompleteAsync();
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
