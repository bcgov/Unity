using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Operations;
using Unity.AI.RateLimit;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications.Automation.Events;
using Volo.Abp;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public class RunApplicationAIPipelineJob(
    IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
    IAttachmentSummaryService attachmentSummaryService,
    IApplicationAnalysisService applicationAnalysisService,
    IApplicationScoringService applicationScoringService,
    IFeatureChecker featureChecker,
    ILocalEventBus localEventBus,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentTenant currentTenant,
    IUnitOfWorkManager unitOfWorkManager,
    IAIRateLimiter aiRateLimiter,
    ILogger<RunApplicationAIPipelineJob> logger) : AsyncBackgroundJob<RunApplicationAIPipelineJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(RunApplicationAIPipelineJobArgs args)
    {
        using var logScope = AIGenerationLogScope.Begin(
            logger,
            AIGenerationRequestKeyHelper.PipelineOperationType,
            args.ApplicationId,
            args.TenantId,
            args.RequestKey,
            args.PromptVersion,
            args.RequestedByUserId);

        if (string.IsNullOrWhiteSpace(args.RequestKey))
        {
            throw new ArgumentException("RequestKey is required.", nameof(args));
        }

        using (currentTenant.Change(args.TenantId))
        {
            await AIGenerationRequestJobHelper.MarkRunningInNewUowAsync(unitOfWorkManager, generationRequestRepository, args.RequestKey);

            try
            {
                var attachmentSummariesEnabled = await featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries");
                var applicationAnalysisEnabled = await featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis");
                var scoringEnabled = await featureChecker.IsEnabledAsync("Unity.AI.Scoring");

                if (!attachmentSummariesEnabled && !applicationAnalysisEnabled && !scoringEnabled)
                {
                    logger.LogDebug("All AI features are disabled, skipping queued AI generation for application {ApplicationId}.", args.ApplicationId);
                    await AIGenerationRequestJobHelper.MarkCompletedInNewUowAsync(unitOfWorkManager, generationRequestRepository, args.RequestKey);
                    await AIGenerationRequestJobHelper.StampRateLimitBestEffortAsync(aiRateLimiter, logger, args.RequestedByUserId, args.ApplicationId, args.RequestKey);
                    return;
                }

                logger.LogInformation("Executing queued AI content pipeline for application {ApplicationId}.", args.ApplicationId);

                if (attachmentSummariesEnabled)
                {
                    var attachmentIds = await GetAttachmentIdsAsync(args.ApplicationId);
                    var attachmentResults = await attachmentSummaryService.GenerateAndSaveAsync(attachmentIds, args.PromptVersion);
                    logger.LogInformation("Completed AI attachment summaries for application {ApplicationId} with {AttachmentCount} result(s).", args.ApplicationId, attachmentResults.Count);
                }

                Exception? analysisException = null;
                Exception? scoringException = null;

                if (applicationAnalysisEnabled)
                {
                    try
                    {
                        await applicationAnalysisService.RegenerateAndSaveAsync(args.ApplicationId, args.PromptVersion);
                        logger.LogInformation("Completed AI application analysis stage for application {ApplicationId}.", args.ApplicationId);
                    }
                    catch (Exception ex)
                    {
                        analysisException = ex;
                        logger.LogError(ex, "Error executing AI application analysis stage for application {ApplicationId}.", args.ApplicationId);
                    }
                }

                if (scoringEnabled)
                {
                    try
                    {
                        await applicationScoringService.RegenerateAndSaveAsync(args.ApplicationId, args.PromptVersion);
                        await localEventBus.PublishAsync(new ApplicationAIScoringGeneratedEvent
                        {
                            ApplicationId = args.ApplicationId
                        });
                    }
                    catch (Exception ex)
                    {
                        scoringException = ex;
                        logger.LogError(ex, "Error executing AI application scoring stage for application {ApplicationId}.", args.ApplicationId);
                    }
                }

                if (scoringException != null)
                {
                    throw scoringException;
                }

                if (analysisException != null)
                {
                    throw analysisException;
                }

                await AIGenerationRequestJobHelper.StampRateLimitBestEffortAsync(aiRateLimiter, logger, args.RequestedByUserId, args.ApplicationId, args.RequestKey);
                await AIGenerationRequestJobHelper.MarkCompletedInNewUowAsync(unitOfWorkManager, generationRequestRepository, args.RequestKey);
            }
            catch (Exception ex)
            {
                await AIGenerationRequestJobHelper.MarkFailedInNewUowAsync(unitOfWorkManager, generationRequestRepository, args.RequestKey, ex.Message);
                throw;
            }
        }
    }

    private async Task<List<Guid>> GetAttachmentIdsAsync(Guid applicationId)
    {
        var attachments = await applicationChefsFileAttachmentRepository.GetListAsync(a => a.ApplicationId == applicationId);
        return attachments.Select(a => a.Id).ToList();
    }
}
