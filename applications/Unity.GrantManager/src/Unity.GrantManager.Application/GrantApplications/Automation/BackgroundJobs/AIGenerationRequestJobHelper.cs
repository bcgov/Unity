using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.AI.Domain;
using Unity.AI.Cooldown;
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
        IRepository<AIOperation, Guid> operationRepository,
        Guid? tenantId,
        Guid applicationId,
        string operationType)
    {
        using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
        var operation = await ResolveOperationAsync(operationRepository, operationType);
        var request = await GetLatestRequestAsync(
            generationRequestRepository,
            x => x.TenantId == tenantId
                 && x.ApplicationId == applicationId
                 && x.OperationId == operation.Id);
        await MarkRunningAsync(generationRequestRepository, request);
        await uow.CompleteAsync();
    }

    public static async Task MarkCompletedInNewUowAsync(
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<AIGenerationRequest, Guid> generationRequestRepository,
        IRepository<AIOperation, Guid> operationRepository,
        Guid? tenantId,
        Guid applicationId,
        string operationType)
    {
        using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
        var operation = await ResolveOperationAsync(operationRepository, operationType);
        var request = await GetLatestRequestAsync(
            generationRequestRepository,
            x => x.TenantId == tenantId
                 && x.ApplicationId == applicationId
                 && x.OperationId == operation.Id);
        await MarkCompletedAsync(generationRequestRepository, request);
        await uow.CompleteAsync();
    }

    public static async Task MarkFailedInNewUowAsync(
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<AIGenerationRequest, Guid> generationRequestRepository,
        IRepository<AIOperation, Guid> operationRepository,
        Guid? tenantId,
        Guid applicationId,
        string operationType,
        string? failureReason)
    {
        using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
        var operation = await ResolveOperationAsync(operationRepository, operationType);
        var request = await GetLatestRequestAsync(
            generationRequestRepository,
            x => x.TenantId == tenantId
                 && x.ApplicationId == applicationId
                 && x.OperationId == operation.Id);
        await MarkFailedAsync(generationRequestRepository, request, failureReason);
        await uow.CompleteAsync();
    }

    public static async Task StampCooldownBestEffortAsync(
        IAICooldownAppService aiCooldownService,
        ILogger logger,
        Guid? requestedByUserId,
        Guid applicationId,
        string operationType)
    {
        try
        {
            await aiCooldownService.StampAsync(requestedByUserId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "AI cooldown stamp failed after completed AI generation request for application {ApplicationId} and operation {OperationType}.",
                applicationId,
                operationType);
        }
    }

    private static async Task<AIOperation> ResolveOperationAsync(
        IRepository<AIOperation, Guid> operationRepository,
        string operationType)
    {
        var operationName = AIGenerationRequestKeyHelper.ResolveOperationName(operationType);
        if (operationName == null)
        {
            throw new ArgumentException($"Unknown AI operation type '{operationType}'.", nameof(operationType));
        }

        var operation = await operationRepository.FirstOrDefaultAsync(item => item.Name == operationName);

        return operation ?? throw new InvalidOperationException($"AI operation '{operationType}' is not configured.");
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
