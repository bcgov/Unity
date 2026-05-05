using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public class GenerateApplicationScoringJob(
    IApplicationScoringAppService applicationScoringAppService,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentTenant currentTenant,
    IUnitOfWorkManager unitOfWorkManager,
    ILogger<GenerateApplicationScoringJob> logger) : AsyncBackgroundJob<GenerateApplicationScoringBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateApplicationScoringBackgroundJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            await AIGenerationRequestJobHelper.MarkRunningInNewUowAsync(unitOfWorkManager, generationRequestRepository, args.RequestKey);
            try
            {
                logger.LogInformation("Executing AI application scoring job for application {ApplicationId}.", args.ApplicationId);
                await applicationScoringAppService.GenerateApplicationScoringAsync(args.ApplicationId, args.PromptVersion);
                logger.LogInformation("Completed AI application scoring job for application {ApplicationId}.", args.ApplicationId);
                await AIGenerationRequestJobHelper.MarkCompletedInNewUowAsync(unitOfWorkManager, generationRequestRepository, args.RequestKey);
            }
            catch (Exception ex)
            {
                await AIGenerationRequestJobHelper.MarkFailedInNewUowAsync(unitOfWorkManager, generationRequestRepository, args.RequestKey, ex.Message);
                throw;
            }
        }
    }
}
