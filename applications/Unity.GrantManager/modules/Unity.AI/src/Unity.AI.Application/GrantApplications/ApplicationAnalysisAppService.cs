using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
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
    public async Task<ApplicationAnalysisResultDto> GenerateApplicationAnalysisAsync(Guid applicationId, string? promptVersion = null)
    {
        if (!await featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis"))
        {
            throw new UserFriendlyException("AI application analysis is not enabled.");
        }

        await applicationAnalysisService.RegenerateAndSaveAsync(applicationId, promptVersion);
        return new ApplicationAnalysisResultDto { Completed = true };
    }
}
