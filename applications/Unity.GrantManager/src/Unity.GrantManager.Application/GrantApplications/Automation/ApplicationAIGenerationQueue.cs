using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Automation;
using Unity.AI.Features;
using Unity.AI.Localization;
using Unity.AI.Operations;
using Unity.AI.Cooldown;
using Unity.AI.Validation;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
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
    IGenerationPrerequisiteValidator aiGenerationPrerequisiteValidator,
    IFeatureChecker featureChecker,
    IAICooldownAppService aiCooldownService,
    IAsyncQueryableExecuter asyncQueryableExecuter,
    ICurrentUser currentUser,
    ILogger<ApplicationGenerationQueue> logger,
    IStringLocalizer<AIResource> localizer)
    : IApplicationGenerationQueue, ITransientDependency
{
    private readonly IAsyncQueryableExecuter _asyncQueryableExecuter = asyncQueryableExecuter;
    public async Task QueueApplicationAttachmentSummaryAsync(Guid applicationId, Guid? tenantId, List<Guid> attachmentIds, string? promptVersion = null)
    {
        await EnsureRequestAndEnqueueAsync(
            tenantId,
            AIGenerationRequestKeyHelper.AttachmentSummaryOperationType,
            applicationId,
            () => aiGenerationPrerequisiteValidator.EnsureAttachmentSummaryAvailableAsync(applicationId),
            () =>
            {
                return backgroundJobManager.EnqueueAsync(new GenerateApplicationAttachmentSummaryBackgroundJobArgs
                {
                    ApplicationId = applicationId,
                    AttachmentIds = attachmentIds,
                    PromptVersion = promptVersion,
                    RequestedByUserId = currentUser.Id,
                    TenantId = tenantId
                });
            });
    }

    public async Task QueueApplicationAnalysisAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null)
    {
        await EnsureRequestAndEnqueueAsync(
            tenantId,
            AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType,
            applicationId,
            () => aiGenerationPrerequisiteValidator.EnsureApplicationAnalysisAvailableAsync(applicationId),
            () =>
            {
                return backgroundJobManager.EnqueueAsync(new GenerateApplicationAnalysisBackgroundJobArgs
                {
                    ApplicationId = applicationId,
                    PromptVersion = promptVersion,
                    RequestedByUserId = currentUser.Id,
                    TenantId = tenantId
                });
            });
    }

    public async Task QueueApplicationScoringAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null)
    {
        await EnsureRequestAndEnqueueAsync(
            tenantId,
            AIGenerationRequestKeyHelper.ApplicationScoringOperationType,
            applicationId,
            () => aiGenerationPrerequisiteValidator.EnsureApplicationScoringAvailableAsync(applicationId),
            () =>
            {
                return backgroundJobManager.EnqueueAsync(new GenerateApplicationScoringBackgroundJobArgs
                {
                    ApplicationId = applicationId,
                    PromptVersion = promptVersion,
                    RequestedByUserId = currentUser.Id,
                    TenantId = tenantId
                });
            });
    }

    public async Task QueueFormMappingAsync(Guid applicationId, Guid? tenantId, Guid applicationFormVersionId, string? promptVersion = null)
    {
        await EnsureRequestAndEnqueueAsync(
            tenantId,
            AIGenerationRequestKeyHelper.FormMappingOperationType,
            applicationId,
            () => aiGenerationPrerequisiteValidator.EnsureFormMappingAvailableAsync(applicationFormVersionId),
            () =>
            {
                return backgroundJobManager.EnqueueAsync(new GenerateFormMappingBackgroundJobArgs
                {
                    ApplicationId = applicationId,
                    ApplicationFormVersionId = applicationFormVersionId,
                    PromptVersion = promptVersion,
                    RequestedByUserId = currentUser.Id,
                    TenantId = tenantId
                });
            });
    }

    public async Task QueueFormWorksheetAsync(Guid applicationId, Guid? tenantId, Guid applicationFormVersionId, string? promptVersion = null)
    {
        await EnsureRequestAndEnqueueAsync(
            tenantId,
            AIGenerationRequestKeyHelper.FormWorksheetOperationType,
            applicationId,
            () => aiGenerationPrerequisiteValidator.EnsureFormWorksheetAvailableAsync(applicationFormVersionId),
            () =>
            {
                return backgroundJobManager.EnqueueAsync(new GenerateFormWorksheetBackgroundJobArgs
                {
                    ApplicationId = applicationId,
                    ApplicationFormVersionId = applicationFormVersionId,
                    PromptVersion = promptVersion,
                    RequestedByUserId = currentUser.Id,
                    TenantId = tenantId
                });
            });
    }

    public async Task QueueFormScoresheetAsync(Guid applicationId, Guid? tenantId, Guid applicationFormVersionId, string? promptVersion = null)
    {
        await EnsureRequestAndEnqueueAsync(
            tenantId,
            AIGenerationRequestKeyHelper.FormScoresheetOperationType,
            applicationId,
            () => aiGenerationPrerequisiteValidator.EnsureFormScoresheetAvailableAsync(applicationFormVersionId),
            () =>
            {
                var requestedByUserId = currentUser.Id
                    ?? throw new UserFriendlyException("A logged-in user is required to generate a form scoresheet.");

                return backgroundJobManager.EnqueueAsync(new GenerateFormScoresheetBackgroundJobArgs
                {
                    ApplicationId = applicationId,
                    ApplicationFormVersionId = applicationFormVersionId,
                    PromptVersion = promptVersion,
                    RequestedByUserId = requestedByUserId,
                    TenantId = tenantId
                });
            });
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
                await QueueApplicationAttachmentSummaryAsync(applicationId, tenantId, new List<Guid>(), promptVersion: promptVersion);
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
                await QueueApplicationAnalysisAsync(applicationId, tenantId, promptVersion);
                enqueuedStage = true;
            }
            catch (UserFriendlyException ex)
            {
                lastStageException = ex;
            }
        }

        if (await featureChecker.IsEnabledAsync(AIFeatures.ApplicationScoring))
        {
            hasEnabledStage = true;
            try
            {
                await QueueApplicationScoringAsync(applicationId, tenantId, promptVersion);
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
        string operationType,
        Guid applicationId,
        Func<Task> validateInput,
        Func<Task> enqueue)
    {
        var operation = await ResolveOperationAsync(operationType);
        var requestLock = distributedLockProvider.CreateLock($"ai-generation:{tenantId}:{applicationId}:{operation.Id}");

        using (await requestLock.AcquireAsync())
        {
            var query = await generationRequestRepository.GetQueryableAsync();
            var existingRequests = query.Where(x =>
                x.TenantId == tenantId
                && x.ApplicationId == applicationId
                && x.OperationId == operation.Id
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

            // Single chokepoint for all AI generate flows (manual + auto).
            // The limiter is a no-op for system/background callers without an authenticated user.
            await aiCooldownService.EnsureAsync();

            var request = new AIGenerationRequest(
                Guid.NewGuid(),
                tenantId,
                operation.Id,
                applicationId);

            await generationRequestRepository.InsertAsync(request, autoSave: true);

            try
            {
                await enqueue();
            }
            catch (Exception ex)
            {
                await MarkFailedBestEffortAsync(request, ex);
                throw;
            }
        }
    }

    private static string ResolveOperationName(string operationType)
        => AIGenerationRequestKeyHelper.ResolveOperationName(operationType)
            ?? throw new UserFriendlyException($"AI operation '{operationType}' is not configured.");

    private async Task<AIOperation> ResolveOperationAsync(string operationType)
    {
        var operationName = ResolveOperationName(operationType);
        var operations = await operationRepository.GetQueryableAsync();
        var operation = await _asyncQueryableExecuter.FirstOrDefaultAsync(
            operations.Where(operation =>
                operation.IsActive &&
                operation.Name == operationName));

        if (operation == null)
        {
            throw new UserFriendlyException($"AI operation '{operationType}' is not configured.");
        }

        if (!operation.IsActive)
        {
            throw new UserFriendlyException($"AI operation '{operationType}' is not configured.");
        }

        return operation;
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
