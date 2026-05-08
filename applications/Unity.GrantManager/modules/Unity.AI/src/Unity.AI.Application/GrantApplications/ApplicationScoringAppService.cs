using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Features;
using Unity.AI.Localization;
using Unity.AI.Operations;
using Unity.AI.Permissions;
using Unity.AI.Settings;
using Unity.GrantManager.GrantApplications.Automation.Events;
using Volo.Abp;
using Volo.Abp.EventBus.Local;

namespace Unity.GrantManager.GrantApplications;

[Authorize(AIPermissions.Analysis.GenerateScoring)]
public class ApplicationScoringAppService(
    IApplicationScoringService applicationScoringService,
    AIFeatureGuard featureGuard,
    ILocalEventBus localEventBus)
    : AIAppService, IApplicationScoringAppService
{
    public virtual async Task<ApplicationScoringResultDto> GenerateApplicationScoringAsync(Guid applicationId, string? promptVersion = null)
    {
        await featureGuard.EnsureEnabledAsync(
            AIFeatures.Scoring,
            AILocalizationKeys.ScoringDisabled);

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
