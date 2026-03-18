using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.Permissions;
using Unity.GrantManager.AI;
using Volo.Abp;
using Volo.Abp.Features;

namespace Unity.GrantManager.GrantApplications;

[Authorize(AIPermissions.ApplicationAnalysis.ApplicationAnalysisDefault)]
public class ApplicationAIAnalysisAppService(
    IApplicationAnalysisService applicationAnalysisService,
    IFeatureChecker featureChecker)
    : GrantManagerAppService, IApplicationAIAnalysisAppService
{
    public async Task<string> GenerateAIAnalysisAsync(Guid applicationId, string? promptVersion = null, bool capturePromptIo = false)
    {
        try
        {
            if (!await featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis"))
            {
                throw new UserFriendlyException("AI application analysis is not enabled.");
            }

            return await applicationAnalysisService.RegenerateAndSaveAsync(applicationId, promptVersion, capturePromptIo);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error regenerating AI analysis for application {ApplicationId}", applicationId);
            throw new UserFriendlyException("Failed to regenerate AI analysis. Please try again.");
        }
    }
}
