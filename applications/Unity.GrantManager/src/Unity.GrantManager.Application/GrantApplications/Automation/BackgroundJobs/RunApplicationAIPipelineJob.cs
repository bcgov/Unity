using Microsoft.Extensions.Logging;
using Medallion.Threading;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Operations;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantApplications.Automation.Events;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public class RunApplicationAIPipelineJob(
    IAIService aiService,
    IAttachmentSummaryService attachmentSummaryService,
    IApplicationAnalysisService applicationAnalysisService,
    IApplicationScoringService applicationScoringService,
    IFeatureChecker featureChecker,
    ILocalEventBus localEventBus,
    ICurrentTenant currentTenant,
    IApplicationRepository applicationRepository,
    IApplicationFormRepository applicationFormRepository,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    IDistributedLockProvider distributedLockProvider,
    ILogger<RunApplicationAIPipelineJob> logger) : AsyncBackgroundJob<RunApplicationAIPipelineJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(RunApplicationAIPipelineJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            var requestKey = string.IsNullOrWhiteSpace(args.RequestKey)
                ? ApplicationAIGenerationQueue.BuildPipelineRequestKey(args.TenantId, args.ApplicationId, args.PromptVersion)
                : args.RequestKey;
            var executionLock = distributedLockProvider.CreateLock($"ai-generation-run:{requestKey}");

            using (await executionLock.AcquireAsync())
            {
                try
                {
                    var request = await AIGenerationRequestJobBase.GetLatestRequestAsync(generationRequestRepository, x =>
                        x.RequestKey == requestKey &&
                        x.ApplicationId == args.ApplicationId);

                    if (request != null && request.Status == AIGenerationRequestStatus.Completed)
                    {
                        logger.LogDebug("AI generation request {RequestKey} is already completed for application {ApplicationId}.", requestKey, args.ApplicationId);
                        return;
                    }

                    await AIGenerationRequestJobBase.MarkRunningAsync(generationRequestRepository, request);

                    var application = await applicationRepository.GetAsync(args.ApplicationId);
                    var applicationForm = await applicationFormRepository.GetAsync(application.ApplicationFormId);

                    if (!applicationForm.AutomaticallyGenerateAIAnalysis)
                    {
                        logger.LogDebug("Automatic AI analysis is disabled at form level for application {ApplicationId}, skipping intake pipeline.", args.ApplicationId);
                        await AIGenerationRequestJobBase.MarkCompletedAsync(generationRequestRepository, request);
                        return;
                    }

                    var attachmentSummariesEnabled = await featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries");
                    var applicationAnalysisEnabled = await featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis");
                    var scoringEnabled = await featureChecker.IsEnabledAsync("Unity.AI.Scoring");
                    if (!attachmentSummariesEnabled && !applicationAnalysisEnabled && !scoringEnabled)
                    {
                        logger.LogDebug("All AI features are disabled, skipping queued AI generation for application {ApplicationId}.", args.ApplicationId);
                        await AIGenerationRequestJobBase.MarkCompletedAsync(generationRequestRepository, request);
                        return;
                    }

                    if (!await aiService.IsAvailableAsync())
                    {
                        logger.LogWarning("AI service is not available, skipping queued AI generation for application {ApplicationId}.", args.ApplicationId);
                        await AIGenerationRequestJobBase.MarkFailedAsync(generationRequestRepository, request, "AI service is not available.");
                        return;
                    }

                    logger.LogInformation("Executing queued AI content pipeline for application {ApplicationId}.", args.ApplicationId);
                    if (attachmentSummariesEnabled)
                    {
                        await attachmentSummaryService.GenerateForApplicationAsync(args.ApplicationId, args.PromptVersion);
                    }

                    Exception? analysisException = null;
                    Exception? scoringException = null;
                    if (applicationAnalysisEnabled)
                    {
                        try
                        {
                            await applicationAnalysisService.RegenerateAndSaveAsync(args.ApplicationId, args.PromptVersion);
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
                            var result = await applicationScoringService.RegenerateAndSaveAsync(args.ApplicationId, args.PromptVersion);
                            if (!string.Equals(result, "{}", StringComparison.Ordinal))
                            {
                                await localEventBus.PublishAsync(new ApplicationAIScoringGeneratedEvent
                                {
                                    ApplicationId = args.ApplicationId
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            scoringException = ex;
                            logger.LogError(ex, "Error executing AI application scoring stage for application {ApplicationId}.", args.ApplicationId);
                        }
                    }

                    if (scoringException != null)
                    {
                        await AIGenerationRequestJobBase.MarkFailedAsync(generationRequestRepository, request, scoringException.Message);
                        throw scoringException;
                    }

                    if (analysisException != null)
                    {
                        await AIGenerationRequestJobBase.MarkFailedAsync(generationRequestRepository, request, analysisException.Message);
                        throw analysisException;
                    }

                    await AIGenerationRequestJobBase.MarkCompletedAsync(generationRequestRepository, request);
                }
                catch (Exception ex)
                {
                    var request = await AIGenerationRequestJobBase.GetLatestRequestAsync(generationRequestRepository, x =>
                        x.RequestKey == requestKey &&
                        x.ApplicationId == args.ApplicationId);
                    await AIGenerationRequestJobBase.MarkFailedAsync(generationRequestRepository, request, ex.Message);
                    throw;
                }
            }
        }
    }
}
