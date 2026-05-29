using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Automation;
using Unity.AI.Features;
using Unity.AI.Localization;
using Unity.AI.Permissions;
using Unity.AI.Settings;
using Volo.Abp;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.GrantApplications;

[Authorize(AIPermissions.Analysis.GenerateApplicationAnalysis)]
public class ApplicationAnalysisAppService(
    Unity.AI.Operations.IApplicationAnalysisService applicationAnalysisService,
    IApplicationAIGenerationQueue aiGenerationQueue,
    AIFeatureGuard featureGuard,
    ICurrentTenant currentTenant)
    : AIAppService, IApplicationAnalysisAppService
{
    public virtual async Task<ApplicationAnalysisResultDto> GenerateApplicationAnalysisAsync(Guid applicationId, string? promptVersion = null)
    {
        await featureGuard.EnsureEnabledAsync(
            AIFeatures.ApplicationAnalysis,
            AILocalizationKeys.ApplicationAnalysisDisabled);

        await aiGenerationQueue.QueueApplicationAnalysisAsync(applicationId, currentTenant.Id, promptVersion);
        return new ApplicationAnalysisResultDto { Completed = false };
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
