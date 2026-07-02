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
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.MultiTenancy;
using Volo.Abp.ObjectMapping;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public class RunApplicationAIPipelineJob(
    IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
    IAIApplicationInputBuilder inputBuilder,
    IAttachmentSummaryService attachmentSummaryService,
    IApplicationAnalysisService applicationAnalysisService,
    IApplicationScoringService applicationScoringService,
    IApplicationRepository applicationRepository,
    IFeatureChecker featureChecker,
    ILocalEventBus localEventBus,
    ICurrentTenant currentTenant,
    IObjectMapper objectMapper,
    ILogger<RunApplicationAIPipelineJob> logger) : AsyncBackgroundJob<RunApplicationAIPipelineJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(RunApplicationAIPipelineJobArgs args)
    {
        using var logScope = AIGenerationLogScope.Begin(
            logger,
            "pipeline",
            args.ApplicationId,
            args.TenantId,
            args.PromptVersion,
            args.RequestedByUserId);

        using (currentTenant.Change(args.TenantId))
        {
            var attachmentSummariesEnabled = await featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries");
            var applicationAnalysisEnabled = await featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis");
            var scoringEnabled = await featureChecker.IsEnabledAsync("Unity.AI.Scoring");
            AIApplicationPromptDataDto? applicationInput = null;

            if (!attachmentSummariesEnabled && !applicationAnalysisEnabled && !scoringEnabled)
            {
                logger.LogDebug("All AI features are disabled, skipping queued AI generation for application {ApplicationId}.", args.ApplicationId);
                return;
            }

            var application = (applicationAnalysisEnabled || scoringEnabled)
                ? await applicationRepository.GetAsync(args.ApplicationId)
                : null;
            applicationInput = application == null
                ? null
                : objectMapper.Map<Application, AIApplicationPromptDataDto>(application);

            if (attachmentSummariesEnabled)
            {
                var attachmentIds = await GetAttachmentIdsAsync(args.ApplicationId);
                if (attachmentIds.Count > 0)
                {
                    await attachmentSummaryService.GenerateAndSaveAsync(attachmentIds, args.PromptVersion);
                }
                else
                {
                    logger.LogDebug("Skipping AI attachment summaries for application {ApplicationId} because no attachments were available.", args.ApplicationId);
                }
            }

            Exception? analysisException = null;
            Exception? scoringException = null;

            if (applicationAnalysisEnabled)
            {
                try
                {
                    var analysisInput = await inputBuilder.BuildApplicationAnalysisInputAsync(applicationInput!, args.PromptVersion);
                    var analysisJson = await applicationAnalysisService.RegenerateAsync(analysisInput);
                    application!.AIAnalysis = analysisJson;
                    await applicationRepository.UpdateAsync(application);
                }
                catch (UserFriendlyException ex)
                {
                    logger.LogDebug(ex, "Skipping AI application analysis stage for application {ApplicationId}.", args.ApplicationId);
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
                    var scoringInput = await inputBuilder.BuildApplicationScoringInputAsync(applicationInput!, args.PromptVersion);
                    var scoresheetAnswers = await applicationScoringService.RegenerateAsync(scoringInput);
                    application!.AIScoresheetAnswers = scoresheetAnswers;
                    await applicationRepository.UpdateAsync(application);
                    await localEventBus.PublishAsync(new ApplicationAIScoringGeneratedEvent
                    {
                        ApplicationId = args.ApplicationId
                    });
                }
                catch (UserFriendlyException ex)
                {
                    logger.LogDebug(ex, "Skipping AI application scoring stage for application {ApplicationId}.", args.ApplicationId);
                }
                catch (Exception ex)
                {
                    scoringException = ex;
                    logger.LogError(ex, "Error executing AI application scoring stage for application {ApplicationId}.", args.ApplicationId);
                }
            }

            if (analysisException != null && scoringException != null)
            {
                throw new AggregateException(
                    $"AI pipeline failed for application {args.ApplicationId} in multiple stages.",
                    analysisException,
                    scoringException);
            }

            if (analysisException != null)
            {
                throw analysisException;
            }

            if (scoringException != null)
            {
                throw scoringException;
            }
        }
    }

    private async Task<List<Guid>> GetAttachmentIdsAsync(Guid applicationId)
    {
        var attachments = await applicationChefsFileAttachmentRepository.GetListAsync(a => a.ApplicationId == applicationId);
        return attachments.Select(a => a.Id).ToList();
    }
}
