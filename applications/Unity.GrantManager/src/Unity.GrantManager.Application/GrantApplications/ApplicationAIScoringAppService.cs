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
public class ApplicationAIScoringAppService(
    IBackgroundJobManager backgroundJobManager,
    IFeatureChecker featureChecker)
    : GrantManagerAppService, IApplicationAIScoringAppService
{
    public async Task<string> GenerateAIScoresheetAnswersAsync(Guid applicationId, string? promptVersion = null, bool capturePromptIo = false)
    {
        try
        {
            if (!await featureChecker.IsEnabledAsync("Unity.AI.Scoring"))
            {
                throw new UserFriendlyException("AI scoring is not enabled.");
            }

            await backgroundJobManager.EnqueueAsync(new GenerateApplicationAIScoresheetBackgroundJobArgs
            {
                ApplicationId = applicationId,
                PromptVersion = promptVersion,
                CapturePromptIo = capturePromptIo,
                TenantId = CurrentTenant.Id
            });

            return "{}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error queueing AI scoresheet generation for application {ApplicationId}", applicationId);
            throw new UserFriendlyException("Failed to queue AI scoresheet generation. Please try again.");
        }
    }
}
