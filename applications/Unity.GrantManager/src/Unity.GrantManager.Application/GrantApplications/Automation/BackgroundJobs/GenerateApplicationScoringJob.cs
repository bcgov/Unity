using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
public class GenerateApplicationScoringJob(
    IApplicationScoringAppService applicationScoringService,
    ICurrentTenant currentTenant,
    ILogger<GenerateApplicationScoringJob> logger) : AsyncBackgroundJob<GenerateApplicationScoringBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateApplicationScoringBackgroundJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            logger.LogInformation("Executing AI application scoring job for application {ApplicationId}.", args.ApplicationId);
            await applicationScoringService.GenerateApplicationScoringAsync(args.ApplicationId, args.PromptVersion);
        }
    }
}
