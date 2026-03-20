using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.AI.Operations;
using Unity.GrantManager.Intakes.Events;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.AI.BackgroundJobs;

public class GenerateApplicationScoringBackgroundJob(
    IApplicationScoringService applicationScoringService,
    ILocalEventBus localEventBus,
    ICurrentTenant currentTenant,
    ILogger<GenerateApplicationScoringBackgroundJob> logger) : AsyncBackgroundJob<GenerateApplicationScoringBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateApplicationScoringBackgroundJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            try
            {
                logger.LogInformation("Executing AI application scoring background job for application {ApplicationId}.", args.ApplicationId);

                var result = await applicationScoringService.RegenerateAndSaveAsync(args.ApplicationId, args.PromptVersion);
                if (!string.Equals(result, "{}", StringComparison.Ordinal))
                {
                    await localEventBus.PublishAsync(new AIApplicationScoringGeneratedEvent
                    {
                        ApplicationId = args.ApplicationId
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing AI application scoring background job for application {ApplicationId}.", args.ApplicationId);
                throw;
            }
        }
    }
}
