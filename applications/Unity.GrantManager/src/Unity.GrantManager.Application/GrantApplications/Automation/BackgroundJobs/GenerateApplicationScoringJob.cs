using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.Operations;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantApplications.Automation.Events;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public class GenerateApplicationScoringJob(
    IApplicationScoringService applicationScoringService,
    ILocalEventBus localEventBus,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentTenant currentTenant,
    ILogger<GenerateApplicationScoringJob> logger) : AsyncBackgroundJob<GenerateApplicationScoringBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateApplicationScoringBackgroundJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            var request = await AIGenerationRequestJobBase.GetLatestRequestAsync(generationRequestRepository, x => x.RequestKey == args.RequestKey);
            await AIGenerationRequestJobBase.MarkRunningAsync(generationRequestRepository, request);
            try
            {
                logger.LogInformation("Executing AI application scoring job for application {ApplicationId}.", args.ApplicationId);
                await applicationScoringService.RegenerateAndSaveAsync(args.ApplicationId, args.PromptVersion);
                await localEventBus.PublishAsync(new ApplicationAIScoringGeneratedEvent
                {
                    ApplicationId = args.ApplicationId
                });

                await AIGenerationRequestJobBase.MarkCompletedAsync(generationRequestRepository, request);
            }
            catch (Exception ex)
            {
                await AIGenerationRequestJobBase.MarkFailedAsync(generationRequestRepository, request, ex.Message);
                throw;
            }
        }
    }
}
