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
    public async Task<string> GenerateAIScoresheetAnswersAsync(Guid applicationId)
    {
        try
        {
            return await applicationScoresheetAnalysisService.RegenerateAndSaveAsync(applicationId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error regenerating AI scoresheet answers for application {ApplicationId}", applicationId);
            throw new UserFriendlyException("Failed to regenerate AI scoresheet answers. Please try again.");
        }
    }
}
