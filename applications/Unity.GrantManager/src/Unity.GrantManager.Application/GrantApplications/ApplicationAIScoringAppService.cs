using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.AI.Permissions;
using Unity.GrantManager.AI;
using Volo.Abp;

namespace Unity.GrantManager.GrantApplications;

[Authorize(AIPermissions.ScoringAssistant.ScoringAssistantDefault)]
public class ApplicationAIScoringAppService(
    IApplicationScoresheetAnalysisService applicationScoresheetAnalysisService)
    : GrantManagerAppService, IApplicationAIScoringAppService
{
    public async Task<string> GenerateAIScoresheetAnswersAsync(Guid applicationId, string? promptVersion = null, bool capturePromptIo = false)
    {
        try
        {
            return await applicationScoresheetAnalysisService.RegenerateAndSaveAsync(applicationId, promptVersion, capturePromptIo);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error regenerating AI scoresheet answers for application {ApplicationId}", applicationId);
            throw new UserFriendlyException("Failed to regenerate AI scoresheet answers. Please try again.");
        }
    }
}
