using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Operations;
using Unity.AI.Permissions;
using Unity.GrantManager.GrantApplications.Automation.Events;
using Volo.Abp;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;

namespace Unity.GrantManager.GrantApplications;

[Authorize(AIPermissions.Analysis.GenerateScoring)]
public class ApplicationScoringAppService(
    IApplicationScoringService applicationScoringService,
    IFeatureChecker featureChecker,
    ILocalEventBus localEventBus)
    : AIAppService, IApplicationScoringAppService
{
    public virtual async Task<ApplicationScoringResultDto> GenerateApplicationScoringAsync(Guid applicationId, string? promptVersion = null)
    {
        if (!await featureChecker.IsEnabledAsync("Unity.AI.Scoring"))
        {
            throw new UserFriendlyException("AI scoring is not enabled.");
        }

        await applicationScoringService.RegenerateAndSaveAsync(applicationId, promptVersion);

        if (UnitOfWorkManager.Current != null)
        {
            var capturedId = applicationId;
            UnitOfWorkManager.Current.OnCompleted(async () =>
            {
                await localEventBus.PublishAsync(new ApplicationAIScoringGeneratedEvent
                {
                    ApplicationId = capturedId
                });
            });
        }
        else
        {
            await localEventBus.PublishAsync(new ApplicationAIScoringGeneratedEvent
            {
                ApplicationId = applicationId
            });
        }

        return new ApplicationScoringResultDto
        {
            Completed = true
        };
    }

    // Internal-only: no HTTP endpoint, no auth check — safe for background job callers
    [AllowAnonymous]
    [RemoteService(IsEnabled = false)]
    public virtual async Task<ApplicationScoringResultDto> GenerateApplicationScoringForPipelineAsync(Guid applicationId, string? promptVersion = null)
    {
        await applicationScoringService.RegenerateAndSaveAsync(applicationId, promptVersion);
        return new ApplicationScoringResultDto { Completed = true };
    }
}
