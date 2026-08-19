using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Generation;
using Unity.AI.Features;
using Unity.AI.Localization;
using Unity.AI.Operations;
using Unity.AI.RateLimit;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Unity.GrantManager.GrantApplications;
using Medallion.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;
using Volo.Abp.Linq;
using Volo.Abp.Users;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.GrantApplications.Automation;

public class ApplicationGenerationQueue(
    IBackgroundJobManager backgroundJobManager,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    IRepository<AIOperation, Guid> operationRepository,
    IDistributedLockProvider distributedLockProvider,
    IAIGenerationPrerequisiteValidator aiGenerationPrerequisiteValidator,
    IFeatureChecker featureChecker,
    IAIRateLimiter aiRateLimiter,
    IAsyncQueryableExecuter asyncQueryableExecuter,
    ICurrentUser currentUser,
    ILogger<ApplicationGenerationQueue> logger)
    : IApplicationGenerationQueue, ITransientDependency
{
    private readonly IAsyncQueryableExecuter _asyncQueryableExecuter = asyncQueryableExecuter;
    public async Task QueueAsync(string operationType, AIGenerationSubmissionDto request, Guid? tenantId)
    {
        var operation = AIGenerationOperations.Get(operationType);

        await EnsureRequestAndEnqueueAsync(
            tenantId,
            operation,
            request,
            () => aiGenerationPrerequisiteValidator.EnsureAvailableAsync(operation.OperationType, request));
    }

    public async Task QueueApplicationIntakeAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null)
    {
        var hasEnabledStage = false;
        var enqueuedStage = false;
        UserFriendlyException? lastStageException = null;

        if (await featureChecker.IsEnabledAsync(AIFeatures.AttachmentSummaries))
        {
            hasEnabledStage = true;
            try
            {
                await QueueAsync(
                    AIGenerationOperations.AttachmentSummary,
                    new AIGenerationSubmissionDto
                    {
                        ApplicationId = applicationId,
                        PromptVersion = promptVersion
                    },
                    tenantId);
                enqueuedStage = true;
            }
            catch (UserFriendlyException ex)
            {
                lastStageException = ex;
            }
        }

        if (await featureChecker.IsEnabledAsync(AIFeatures.ApplicationAnalysis))
        {
            hasEnabledStage = true;
            try
            {
                await QueueAsync(
                    AIGenerationOperations.ApplicationAnalysis,
                    new AIGenerationSubmissionDto
                    {
                        ApplicationId = applicationId,
                        PromptVersion = promptVersion
                    },
                    tenantId);
                enqueuedStage = true;
            }
            catch (UserFriendlyException ex)
            {
                lastStageException = ex;
            }
        }

        if (await featureChecker.IsEnabledAsync(AIFeatures.Scoring))
        {
            hasEnabledStage = true;
            try
            {
                await QueueAsync(
                    AIGenerationOperations.ApplicationScoring,
                    new AIGenerationSubmissionDto
                    {
                        ApplicationId = applicationId,
                        PromptVersion = promptVersion
                    },
                    tenantId);
                enqueuedStage = true;
            }
            catch (UserFriendlyException ex)
            {
                lastStageException = ex;
            }
        }

        if (!hasEnabledStage)
        {
            throw new UserFriendlyException("No AI generation features are enabled.");
        }

        if (!enqueuedStage && lastStageException != null)
        {
            throw lastStageException;
        }
    }

    private async Task EnsureRequestAndEnqueueAsync(
        Guid? tenantId,
        AIGenerationOperationDefinition operation,
        AIGenerationSubmissionDto request,
        Func<Task> validateInput)
    {
        var persistedOperation = await ResolveOperationAsync(operation);
        var requestLock = distributedLockProvider.CreateLock($"ai-generation:{tenantId}:{request.ApplicationId}:{persistedOperation.Id}");

        // The lock must cover the active-request check so each tenant/application/operation queues only once.
        using (await requestLock.AcquireAsync())
        {
            var query = await generationRequestRepository.GetQueryableAsync();
            var existingRequests = query.Where(x =>
                x.TenantId == tenantId
                && x.ApplicationId == request.ApplicationId
                && x.OperationId == persistedOperation.Id
                && (x.Status == AIGenerationRequestStatus.Queued || x.Status == AIGenerationRequestStatus.Running));

            var existing = await _asyncQueryableExecuter.FirstOrDefaultAsync(
                existingRequests
                    .OrderByDescending(x => x.CreationTime)
                    .ThenByDescending(x => x.Id));

            if (existing != null)
            {
                return;
            }

            await validateInput();

            // Manual and automatic flows share this user-scoped limiter; system callers bypass it.
            await aiRateLimiter.EnsureAsync(currentUser.Id);

            var generationRequest = new AIGenerationRequest(
                Guid.NewGuid(),
                tenantId,
                persistedOperation.Id,
                request.ApplicationId);

            await generationRequestRepository.InsertAsync(generationRequest, autoSave: true);

            try
            {
                await backgroundJobManager.EnqueueAsync(new AIGenerationBackgroundJobArgs
                {
                    OperationType = operation.OperationType,
                    ApplicationId = request.ApplicationId,
                    ApplicationFormVersionId = request.ApplicationFormVersionId,
                    AttachmentIds = request.AttachmentIds,
                    OperationId = persistedOperation.Id,
                    GenerationRequestId = generationRequest.Id,
                    PromptVersion = request.PromptVersion,
                    RequestedByUserId = currentUser.Id,
                    TenantId = tenantId
                });
            }
            catch (Exception ex)
            {
                await MarkFailedBestEffortAsync(generationRequest, ex);
                throw;
            }
        }
    }

    private async Task<AIOperation> ResolveOperationAsync(AIGenerationOperationDefinition operation)
    {
        var operationName = operation.OperationName;
        var operations = await operationRepository.GetQueryableAsync();
        var persistedOperation = await _asyncQueryableExecuter.FirstOrDefaultAsync(
            operations.Where(candidate =>
                candidate.IsActive &&
                candidate.Name == operationName));

        if (persistedOperation == null)
        {
            throw new UserFriendlyException($"AI operation '{operation.OperationType}' is not configured.");
        }

        if (!persistedOperation.IsActive)
        {
            throw new UserFriendlyException($"AI operation '{operation.OperationType}' is not configured.");
        }

        return persistedOperation;
    }

    private async Task MarkFailedBestEffortAsync(AIGenerationRequest request, Exception exception)
    {
        try
        {
            await AIGenerationRequestJobHelper.MarkFailedAsync(generationRequestRepository, request, exception.Message);
        }
        catch (Exception markException)
        {
            logger.LogError(
                markException,
                "Failed to mark AI generation request {RequestId} as failed after enqueue failure.",
                request.Id);
        }
    }
}
