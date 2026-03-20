using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.AI.Operations;
using Unity.GrantManager.Intakes.Events;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.AI.BackgroundJobs;

public class GenerateContentBackgroundJob(
    IAttachmentSummaryService attachmentSummaryService,
    IApplicationAnalysisService applicationAnalysisService,
    IApplicationScoringService applicationScoringService,
    IAIService aiService,
    IFeatureChecker featureChecker,
    ILocalEventBus localEventBus,
    ICurrentTenant currentTenant,
    ILogger<GenerateContentBackgroundJob> logger) : AsyncBackgroundJob<GenerateContentBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateContentBackgroundJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            try
            {
                var attachmentSummariesEnabled = await featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries");
                var applicationAnalysisEnabled = await featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis");
                var scoringEnabled = await featureChecker.IsEnabledAsync("Unity.AI.Scoring");

                if (!attachmentSummariesEnabled && !applicationAnalysisEnabled && !scoringEnabled)
                {
                    logger.LogDebug("All AI features are disabled, skipping queued AI generation for application {ApplicationId}.", args.ApplicationId);
                    return;
                }

                if (!await aiService.IsAvailableAsync())
                {
                    logger.LogWarning("AI service is not available, skipping queued AI generation for application {ApplicationId}.", args.ApplicationId);
                    return;
                }

                logger.LogInformation("Executing queued AI content pipeline for application {ApplicationId}.", args.ApplicationId);

                if (attachmentSummariesEnabled)
                {
                    await attachmentSummaryService.GenerateForApplicationAsync(args.ApplicationId, args.PromptVersion);
                }

                if (applicationAnalysisEnabled)
                {
                    await applicationAnalysisService.RegenerateAndSaveAsync(args.ApplicationId, args.PromptVersion);
                }

                if (scoringEnabled)
                {
                    var result = await applicationScoringService.RegenerateAndSaveAsync(args.ApplicationId, args.PromptVersion);
                    if (!string.Equals(result, "{}", StringComparison.Ordinal))
                    {
                        await localEventBus.PublishAsync(new AIApplicationScoringGeneratedEvent
                        {
                            ApplicationId = args.ApplicationId
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing queued AI content pipeline for application {ApplicationId}.", args.ApplicationId);
            }
        }
    }
}
