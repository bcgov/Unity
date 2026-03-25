using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Unity.GrantManager.AI.Operations;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.AI.BackgroundJobs;

public class GenerateApplicationAnalysisBackgroundJob(
    IApplicationAnalysisService applicationAnalysisService,
    ICurrentTenant currentTenant,
    ILogger<GenerateApplicationAnalysisBackgroundJob> logger) : AsyncBackgroundJob<GenerateApplicationAnalysisBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateApplicationAnalysisBackgroundJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            logger.LogInformation("Executing AI application analysis background job for application {ApplicationId}.", args.ApplicationId);
            await applicationAnalysisService.RegenerateAndSaveAsync(args.ApplicationId, args.PromptVersion);
        }
    }
}
