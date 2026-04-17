using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Operations;
using Unity.AI.Permissions;
using Volo.Abp;
using Volo.Abp.Features;

namespace Unity.GrantManager.GrantApplications;

[Authorize(AIPermissions.Analysis.ViewScoringResult)]
public class ApplicationScoringAppService(
    IApplicationScoringService applicationScoringService,
    IFeatureChecker featureChecker)
    : AIAppService, IApplicationScoringAppService
{
    public async Task<ApplicationScoringResultDto> GenerateApplicationScoringAsync(Guid applicationId, string? promptVersion = null)
    {
        if (!await featureChecker.IsEnabledAsync("Unity.AI.Scoring"))
        {
            throw new UserFriendlyException("AI scoring is not enabled.");
        }

        await applicationScoringService.RegenerateAndSaveAsync(applicationId, promptVersion);
        return new ApplicationScoringResultDto
        {
            Completed = true
        };
    }
}
