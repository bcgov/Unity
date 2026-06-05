using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Automation;
using Unity.AI.Features;
using Unity.AI.Localization;
using Unity.AI.Operations;
using Unity.AI.RateLimit;
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
using Volo.Abp.Users;

namespace Unity.GrantManager.GrantApplications.Automation;

public class ApplicationAIGenerationQueue(
    IBackgroundJobManager backgroundJobManager,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    IDistributedLockProvider distributedLockProvider,
    IAIGenerationPrerequisiteValidator aiGenerationPrerequisiteValidator,
    IFeatureChecker featureChecker,
    IAIRateLimiter aiRateLimiter,
    ICurrentUser currentUser,
    ILogger<ApplicationAIGenerationQueue> logger,
    IStringLocalizer<AIResource> localizer)
    : IApplicationAIGenerationQueue, ITransientDependency
{
    public async Task QueueAttachmentSummaryAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null, List<Guid>? attachmentIds = null)
    {
        var requestKey = AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.AttachmentSummaryOperationType);
        await EnsureRequestAndEnqueueAsync(
            requestKey,
            tenantId,
            AIGenerationRequestKeyHelper.AttachmentSummaryOperationType,
            applicationId,
            () => aiGenerationPrerequisiteValidator.EnsureAttachmentSummaryAvailableAsync(applicationId),
            () =>
            {
                return backgroundJobManager.EnqueueAsync(new GenerateAttachmentSummaryBackgroundJobArgs
                {
                    ApplicationId = applicationId,
                    AttachmentIds = attachmentIds,
                    PromptVersion = promptVersion,
                    RequestedByUserId = currentUser.Id,
                    TenantId = tenantId,
                    RequestKey = requestKey
                });
            });
    }

    public async Task QueueApplicationAnalysisAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null)
    {
        var requestKey = AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType);
        await EnsureRequestAndEnqueueAsync(
            requestKey,
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
                    TenantId = tenantId,
                    RequestKey = requestKey
                });
            });
    }

    public async Task QueueApplicationScoringAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null)
    {
        var requestKey = AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.ApplicationScoringOperationType);
        await EnsureRequestAndEnqueueAsync(
            requestKey,
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
                    TenantId = tenantId,
                    RequestKey = requestKey
                });
            });
    }

    public async Task QueueAllAIStagesAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null)
    {
        var requestKey = AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.PipelineOperationType);
        await EnsureRequestAndEnqueueAsync(
            requestKey,
            tenantId,
            AIGenerationRequestKeyHelper.PipelineOperationType,
            applicationId,
            () => EnsureAnyPipelineStageAvailableAsync(applicationId),
            () =>
            {
                return backgroundJobManager.EnqueueAsync(new RunApplicationAIPipelineJobArgs
                {
                    ApplicationId = applicationId,
                    PromptVersion = promptVersion,
                    RequestedByUserId = currentUser.Id,
                    TenantId = tenantId,
                    RequestKey = requestKey
                });
            });
    }

    private async Task EnsureRequestAndEnqueueAsync(
        string requestKey,
        Guid? tenantId,
        string operationType,
        Guid? applicationId,
        Func<Task> validateInput,
        Func<Task> enqueue)
    {
        var requestLock = distributedLockProvider.CreateLock($"ai-generation:{requestKey}");

        using (await requestLock.AcquireAsync())
        {
            var query = await generationRequestRepository.GetQueryableAsync();
            var existingRequests = query.Where(x =>
                x.RequestKey == requestKey
                && (x.Status == AIGenerationRequestStatus.Queued || x.Status == AIGenerationRequestStatus.Running));

            var existing = existingRequests
                .OrderByDescending(x => x.CreationTime)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();

            if (existing != null)
            {
                return;
            }

            await validateInput();

            // Single chokepoint for all AI generate flows (manual + auto).
            // The limiter is a no-op for system/background callers without an authenticated user.
            await aiRateLimiter.EnsureAsync();

            var request = new AIGenerationRequest(
                Guid.NewGuid(),
                tenantId,
                operationType,
                applicationId,
                requestKey);

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

    private async Task EnsureAnyPipelineStageAvailableAsync(Guid applicationId)
    {
        var hasEnabledStage = false;
        var hasAvailableStage = false;
        UserFriendlyException? lastPrerequisiteException = null;

        async Task CheckStageAsync(bool isEnabled, Func<Task> ensurePrerequisite)
        {
            if (!isEnabled)
            {
                return;
            }

            hasEnabledStage = true;

            try
            {
                await ensurePrerequisite();
                hasAvailableStage = true;
            }
            catch (UserFriendlyException ex)
            {
                lastPrerequisiteException = ex;
            }
        }

        await CheckStageAsync(
            await featureChecker.IsEnabledAsync(AIFeatures.AttachmentSummaries),
            () => aiGenerationPrerequisiteValidator.EnsureAttachmentSummaryAvailableAsync(applicationId));
        await CheckStageAsync(
            await featureChecker.IsEnabledAsync(AIFeatures.ApplicationAnalysis),
            () => aiGenerationPrerequisiteValidator.EnsureApplicationAnalysisAvailableAsync(applicationId));
        await CheckStageAsync(
            await featureChecker.IsEnabledAsync(AIFeatures.Scoring),
            () => aiGenerationPrerequisiteValidator.EnsureApplicationScoringAvailableAsync(applicationId));

        if (hasAvailableStage)
        {
            return;
        }

        if (lastPrerequisiteException != null)
        {
            throw lastPrerequisiteException;
        }

        if (!hasEnabledStage)
        {
            throw new UserFriendlyException(localizer[AILocalizationKeys.GenerateAllDisabled]);
        }
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
