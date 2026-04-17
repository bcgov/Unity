using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantApplications.Automation.Events;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.MultiTenancy;
namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
public class GenerateApplicationScoringJob(
    IApplicationScoringAppService applicationScoringService,
    ILocalEventBus localEventBus,
    ICurrentTenant currentTenant,
    ILogger<GenerateApplicationScoringJob> logger) : AsyncBackgroundJob<GenerateApplicationScoringBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateApplicationScoringBackgroundJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            logger.LogInformation("Executing AI application scoring job for application {ApplicationId}.", args.ApplicationId);
            var result = await applicationScoringService.GenerateApplicationScoringAsync(args.ApplicationId, args.PromptVersion);
            if (result.Completed)
            {
                await localEventBus.PublishAsync(new ApplicationAIScoringGeneratedEvent
                {
                    ApplicationId = args.ApplicationId
                });
            }
        }
    }
}
