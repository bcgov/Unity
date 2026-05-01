using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Attachments;
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
    IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
    IAttachmentSummaryAppService attachmentSummaryAppService,
    IApplicationAnalysisAppService applicationAnalysisAppService,
    IApplicationScoringAppService applicationScoringAppService,
    IFeatureChecker featureChecker,
    ILocalEventBus localEventBus,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentTenant currentTenant,
    ILogger<RunApplicationAIPipelineJob> logger) : AsyncBackgroundJob<RunApplicationAIPipelineJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(RunApplicationAIPipelineJobArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.RequestKey))
        {
            throw new ArgumentException("RequestKey is required.", nameof(args));
        }

        using (currentTenant.Change(args.TenantId))
        {
            var request = await AIGenerationRequestJobHelper.GetLatestRequestAsync(
                generationRequestRepository,
                x => x.RequestKey == args.RequestKey);

            await AIGenerationRequestJobHelper.MarkRunningAsync(generationRequestRepository, request);

            try
            {
                var attachmentSummariesEnabled = await featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries");
                var applicationAnalysisEnabled = await featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis");
                var scoringEnabled = await featureChecker.IsEnabledAsync("Unity.AI.Scoring");

                if (!attachmentSummariesEnabled && !applicationAnalysisEnabled && !scoringEnabled)
                {
                    logger.LogDebug("All AI features are disabled, skipping queued AI generation for application {ApplicationId}.", args.ApplicationId);
                    await AIGenerationRequestJobHelper.MarkCompletedAsync(generationRequestRepository, request);
                    return;
                }

                logger.LogInformation("Executing queued AI content pipeline for application {ApplicationId}.", args.ApplicationId);

                if (attachmentSummariesEnabled)
                {
                    var attachmentIds = await GetAttachmentIdsAsync(args.ApplicationId);
                    var attachmentResults = await attachmentSummaryAppService.GenerateAttachmentSummariesForPipelineAsync(attachmentIds, args.PromptVersion);
                    logger.LogInformation("Completed AI attachment summaries for application {ApplicationId} with {AttachmentCount} result(s).", args.ApplicationId, attachmentResults.Count);
                }

                Exception? analysisException = null;
                Exception? scoringException = null;

                if (applicationAnalysisEnabled)
                {
                    try
                    {
                        var analysisResult = await applicationAnalysisAppService.GenerateApplicationAnalysisForPipelineAsync(args.ApplicationId, args.PromptVersion);
                        if (analysisResult.Completed)
                        {
                            logger.LogInformation("Completed AI application analysis stage for application {ApplicationId}.", args.ApplicationId);
                        }
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
                        var result = await applicationScoringAppService.GenerateApplicationScoringForPipelineAsync(args.ApplicationId, args.PromptVersion);
                        if (result.Completed)
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
                    throw scoringException;
                }

                if (analysisException != null)
                {
                    throw analysisException;
                }

                await AIGenerationRequestJobHelper.MarkCompletedAsync(generationRequestRepository, request);
            }
            catch (Exception ex)
            {
                await AIGenerationRequestJobHelper.MarkFailedAsync(generationRequestRepository, request, ex.Message);
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
