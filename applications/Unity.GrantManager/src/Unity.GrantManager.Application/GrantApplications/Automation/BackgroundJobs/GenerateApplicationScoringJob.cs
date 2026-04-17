using Microsoft.Extensions.Logging;
using System;
using System.Linq;
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
            await MarkRunningAsync(args.RequestKey);
            try
            {
                logger.LogInformation("Executing AI application scoring job for application {ApplicationId}.", args.ApplicationId);
                await applicationScoringService.RegenerateAndSaveAsync(args.ApplicationId, args.PromptVersion);
                await localEventBus.PublishAsync(new ApplicationAIScoringGeneratedEvent
                {
                    ApplicationId = args.ApplicationId
                });

                await MarkCompletedAsync(args.RequestKey);
            }
            catch (Exception ex)
            {
                await MarkFailedAsync(args.RequestKey, ex.Message);
                throw;
            }
        }
    }

    private async Task MarkRunningAsync(string requestKey)
    {
        var request = await GetRequestAsync(requestKey);
        if (request == null)
        {
            return;
        }

        request.MarkRunning(DateTime.UtcNow);
        await generationRequestRepository.UpdateAsync(request, autoSave: true);
    }

    private async Task MarkCompletedAsync(string requestKey)
    {
        var request = await GetRequestAsync(requestKey);
        if (request == null)
        {
            return;
        }

        request.MarkCompleted(DateTime.UtcNow);
        await generationRequestRepository.UpdateAsync(request, autoSave: true);
    }

    private async Task MarkFailedAsync(string requestKey, string? failureReason)
    {
        var request = await GetRequestAsync(requestKey);
        if (request == null)
        {
            return;
        }

        request.MarkFailed(DateTime.UtcNow, failureReason);
        await generationRequestRepository.UpdateAsync(request, autoSave: true);
    }

    private async Task<AIGenerationRequest?> GetRequestAsync(string requestKey)
    {
        var query = await generationRequestRepository.GetQueryableAsync();
        return query
            .Where(x => x.RequestKey == requestKey)
            .OrderByDescending(x => x.CreationTime)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();
    }
}
