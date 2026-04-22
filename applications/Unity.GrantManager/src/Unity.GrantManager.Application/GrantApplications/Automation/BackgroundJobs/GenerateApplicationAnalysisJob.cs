using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
public class GenerateApplicationAnalysisJob(
    IApplicationAnalysisAppService applicationAnalysisService,
    ICurrentTenant currentTenant,
    ILogger<GenerateApplicationAnalysisJob> logger) : AsyncBackgroundJob<GenerateApplicationAnalysisBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateApplicationAnalysisBackgroundJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            logger.LogInformation("Executing AI application analysis job for application {ApplicationId}.", args.ApplicationId);
            var result = await applicationAnalysisService.GenerateApplicationAnalysisAsync(args.ApplicationId, args.PromptVersion);
            if (result.Completed)
            {
                logger.LogInformation("Completed AI application analysis job for application {ApplicationId}.", args.ApplicationId);
            }
        }
    }
}
