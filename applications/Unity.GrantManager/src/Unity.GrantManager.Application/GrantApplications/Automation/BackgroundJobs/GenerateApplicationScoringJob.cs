using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public class GenerateApplicationScoringJob(
    IApplicationScoringAppService applicationScoringAppService,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentTenant currentTenant,
    ILogger<GenerateApplicationScoringJob> logger) : AsyncBackgroundJob<GenerateApplicationScoringBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateApplicationScoringBackgroundJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            var request = await AIGenerationRequestJobHelper.GetLatestRequestAsync(generationRequestRepository, x => x.RequestKey == args.RequestKey);
            await AIGenerationRequestJobHelper.MarkRunningAsync(generationRequestRepository, request);
            try
            {
                logger.LogInformation("Executing AI application scoring job for application {ApplicationId}.", args.ApplicationId);
                await applicationScoringAppService.GenerateApplicationScoringAsync(args.ApplicationId, args.PromptVersion);
                logger.LogInformation("Completed AI application scoring job for application {ApplicationId}.", args.ApplicationId);
                await AIGenerationRequestJobHelper.MarkCompletedAsync(generationRequestRepository, request);
            }
            catch (Exception ex)
            {
                await AIGenerationRequestJobHelper.MarkFailedAsync(generationRequestRepository, request, ex.Message);
                throw;
            }
        }
    }
}
