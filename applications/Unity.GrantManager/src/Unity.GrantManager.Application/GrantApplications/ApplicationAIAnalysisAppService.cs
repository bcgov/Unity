using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.AI;
using Volo.Abp;

namespace Unity.GrantManager.GrantApplications;

public class ApplicationAIAnalysisAppService(
    IApplicationAnalysisService applicationAnalysisService)
    : GrantManagerAppService, IApplicationAIAnalysisAppService
{
    public async Task<string> GenerateAIAnalysisAsync(Guid applicationId, string? promptVersion = null, bool capturePromptIo = false)
    {
        try
        {
            return await applicationAnalysisService.RegenerateAndSaveAsync(applicationId, promptVersion, capturePromptIo);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error regenerating AI analysis for application {ApplicationId}", applicationId);
            throw new UserFriendlyException("Failed to regenerate AI analysis. Please try again.");
        }
    }
}
