using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.Permissions;
using Unity.GrantManager.AI.BackgroundJobs;
using Volo.Abp;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Features;

namespace Unity.GrantManager.GrantApplications;

[Authorize(AIPermissions.ScoringAssistant.ScoringAssistantDefault)]
public class ApplicationScoringAppService(
    IBackgroundJobManager backgroundJobManager,
    IFeatureChecker featureChecker)
    : GrantManagerAppService, IApplicationScoringAppService
{
    public async Task<string> GenerateApplicationScoringAsync(Guid applicationId, string? promptVersion = null)
    {
        try
        {
            if (!await featureChecker.IsEnabledAsync("Unity.AI.Scoring"))
            {
                throw new UserFriendlyException("AI scoring is not enabled.");
            }

            await backgroundJobManager.EnqueueAsync(new GenerateApplicationScoringBackgroundJobArgs
            {
                ApplicationId = applicationId,
                PromptVersion = promptVersion,
                TenantId = CurrentTenant.Id
            });

            return "{}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error queueing AI application scoring generation for application {ApplicationId}", applicationId);
            throw new UserFriendlyException("Failed to queue AI application scoring generation. Please try again.");
        }
    }
}

