using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Operations;
using Unity.AI.Permissions;
using Volo.Abp;
using Volo.Abp.Features;

namespace Unity.GrantManager.GrantApplications;

[Authorize(AIPermissions.Analysis.GenerateApplicationAnalysis)]
public class ApplicationAnalysisAppService(
    IApplicationAnalysisService applicationAnalysisService,
    IFeatureChecker featureChecker)
    : AIAppService, IApplicationAnalysisAppService
{
    public virtual async Task<ApplicationAnalysisResultDto> GenerateApplicationAnalysisAsync(Guid applicationId, string? promptVersion = null)
    {
        if (!await featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis"))
        {
            throw new UserFriendlyException("AI application analysis is not enabled.");
        }

        await applicationAnalysisService.RegenerateAndSaveAsync(applicationId, promptVersion);
        return new ApplicationAnalysisResultDto { Completed = true };
    }

    // Internal-only: no HTTP endpoint, no auth check — safe for background job callers
    [AllowAnonymous]
    [RemoteService(IsEnabled = false)]
    public virtual async Task<ApplicationAnalysisResultDto> GenerateApplicationAnalysisForPipelineAsync(Guid applicationId, string? promptVersion = null)
    {
        await applicationAnalysisService.RegenerateAndSaveAsync(applicationId, promptVersion);
        return new ApplicationAnalysisResultDto { Completed = true };
    }
}
