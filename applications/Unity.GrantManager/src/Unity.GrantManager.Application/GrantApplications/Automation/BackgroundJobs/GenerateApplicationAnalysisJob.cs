using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Unity.AI.Operations;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
public class GenerateApplicationAnalysisJob(
    IApplicationAnalysisService applicationAnalysisService,
    ICurrentTenant currentTenant,
    ILogger<GenerateApplicationAnalysisJob> logger) : AsyncBackgroundJob<GenerateApplicationAnalysisBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateApplicationAnalysisBackgroundJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            logger.LogInformation("Executing AI application analysis job for application {ApplicationId}.", args.ApplicationId);
            await applicationAnalysisService.RegenerateAndSaveAsync(args.ApplicationId, args.PromptVersion);
        }
    }
}