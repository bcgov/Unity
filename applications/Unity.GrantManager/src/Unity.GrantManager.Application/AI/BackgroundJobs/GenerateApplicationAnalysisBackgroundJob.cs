using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.AI.BackgroundJobs;

public class GenerateApplicationAIAnalysisBackgroundJob(
    IApplicationAIAnalysisService applicationAnalysisService,
    ICurrentTenant currentTenant,
    ILogger<GenerateApplicationAIAnalysisBackgroundJob> logger) : AsyncBackgroundJob<GenerateApplicationAIAnalysisBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateApplicationAIAnalysisBackgroundJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            try
            {
                logger.LogInformation("Executing AI application analysis background job for application {ApplicationId}.", args.ApplicationId);
                await applicationAnalysisService.RegenerateAndSaveAsync(args.ApplicationId, args.PromptVersion);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing AI application analysis background job for application {ApplicationId}.", args.ApplicationId);
            }
        }
    }
}


