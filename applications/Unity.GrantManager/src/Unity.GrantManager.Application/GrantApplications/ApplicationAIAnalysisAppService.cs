using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.AI.BackgroundJobs;
using Unity.AI.Permissions;
using Volo.Abp;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Features;

namespace Unity.GrantManager.GrantApplications;

[Authorize(AIPermissions.ApplicationAnalysis.ApplicationAnalysisDefault)]
public class ApplicationAIAnalysisAppService(
    IBackgroundJobManager backgroundJobManager,
    IFeatureChecker featureChecker)
    : GrantManagerAppService, IApplicationAIAnalysisAppService
{
    public async Task<string> GenerateAIAnalysisAsync(Guid applicationId, string? promptVersion = null)
    {
        try
        {
            if (!await featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis"))
            {
                throw new UserFriendlyException("AI application analysis is not enabled.");
            }

            await backgroundJobManager.EnqueueAsync(new GenerateApplicationAIAnalysisBackgroundJobArgs
            {
                ApplicationId = applicationId,
                PromptVersion = promptVersion,
                TenantId = CurrentTenant.Id
            });

            return "{}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error queueing AI analysis for application {ApplicationId}", applicationId);
            throw new UserFriendlyException("Failed to queue AI analysis. Please try again.");
        }
    }
}

