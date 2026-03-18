using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Intakes.Events;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.AI.BackgroundJobs;

public class GenerateApplicationAIScoresheetBackgroundJob(
    IApplicationAIScoringService applicationScoresheetAnalysisService,
    ILocalEventBus localEventBus,
    ICurrentTenant currentTenant,
    ILogger<GenerateApplicationAIScoresheetBackgroundJob> logger) : AsyncBackgroundJob<GenerateApplicationAIScoresheetBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateApplicationAIScoresheetBackgroundJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            try
            {
                logger.LogInformation("Executing AI scoresheet background job for application {ApplicationId}.", args.ApplicationId);

                var result = await applicationScoresheetAnalysisService.RegenerateAndSaveAsync(args.ApplicationId, args.PromptVersion, args.CapturePromptIo);
                if (!string.Equals(result, "{}", StringComparison.Ordinal))
                {
                    await localEventBus.PublishAsync(new AiScoresheetAnswersGeneratedEvent
                    {
                        ApplicationId = args.ApplicationId
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing AI scoresheet background job for application {ApplicationId}.", args.ApplicationId);
            }
        }
    }
}
