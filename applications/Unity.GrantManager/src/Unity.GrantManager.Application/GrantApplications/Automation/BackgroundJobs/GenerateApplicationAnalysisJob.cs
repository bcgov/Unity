using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Operations;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public class GenerateApplicationAnalysisJob(
    IApplicationAnalysisService applicationAnalysisService,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentTenant currentTenant,
    ILogger<GenerateApplicationAnalysisJob> logger) : AsyncBackgroundJob<GenerateApplicationAnalysisBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateApplicationAnalysisBackgroundJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            await MarkRunningAsync(args.RequestKey);
            try
            {
                logger.LogInformation("Executing AI application analysis job for application {ApplicationId}.", args.ApplicationId);
                await applicationAnalysisService.RegenerateAndSaveAsync(args.ApplicationId, args.PromptVersion);
                logger.LogInformation("Completed AI application analysis job for application {ApplicationId}.", args.ApplicationId);

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
