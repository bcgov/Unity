using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.Settings;
using Unity.GrantManager.GrantApplications.Automation.Events;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
public class RunApplicationAIPipelineJob(
    AIOperationServices aiOperationServices,
    IFeatureChecker featureChecker,
    ILocalEventBus localEventBus,
    ICurrentTenant currentTenant,
    ISettingProvider settingProvider,
    ILogger<RunApplicationAIPipelineJob> logger) : AsyncBackgroundJob<RunApplicationAIPipelineJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(RunApplicationAIPipelineJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            var automaticGenerationEnabled = await settingProvider.GetAsync<bool>(
                AISettings.AutomaticGenerationEnabled, defaultValue: false);

            if (!automaticGenerationEnabled)
            {
                logger.LogDebug("Automatic AI generation is disabled at tenant level, skipping intake pipeline for application {ApplicationId}.", args.ApplicationId);
                return;
            }

            var attachmentSummariesEnabled = await featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries");
            var applicationAnalysisEnabled = await featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis");
            var scoringEnabled = await featureChecker.IsEnabledAsync("Unity.AI.Scoring");
            if (!attachmentSummariesEnabled && !applicationAnalysisEnabled && !scoringEnabled)
            {
                logger.LogDebug("All AI features are disabled, skipping queued AI generation for application {ApplicationId}.", args.ApplicationId);
                return;
            }
            if (!await aiOperationServices.AI.IsAvailableAsync())
            {
                logger.LogWarning("AI service is not available, skipping queued AI generation for application {ApplicationId}.", args.ApplicationId);
                return;
            }
            logger.LogInformation("Executing queued AI content pipeline for application {ApplicationId}.", args.ApplicationId);
            if (attachmentSummariesEnabled)
            {
                await aiOperationServices.AttachmentSummary.GenerateForApplicationAsync(args.ApplicationId, args.PromptVersion);
            }
            Exception? analysisException = null;
            Exception? scoringException = null;
            if (applicationAnalysisEnabled)
            {
                try
                {
                    await aiOperationServices.ApplicationAnalysis.RegenerateAndSaveAsync(args.ApplicationId, args.PromptVersion);
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
                    var result = await aiOperationServices.ApplicationScoring.RegenerateAndSaveAsync(args.ApplicationId, args.PromptVersion);
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
                throw scoringException;
            }
            if (analysisException != null)
            {
                throw analysisException;
            }
        }
    }
}
