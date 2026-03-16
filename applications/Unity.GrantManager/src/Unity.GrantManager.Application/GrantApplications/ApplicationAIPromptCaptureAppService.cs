using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.AI.Permissions;
using Unity.GrantManager.AI;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Features;

namespace Unity.GrantManager.GrantApplications
{
    public class ApplicationAIPromptCaptureAppService(
        IAIPromptCaptureStore promptIoCaptureStore,
        IWebHostEnvironment webHostEnvironment,
        IFeatureChecker featureChecker,
        IPermissionChecker permissionChecker)
        : GrantManagerAppService, IApplicationAIPromptCaptureAppService
    {
        public async Task<List<AIPromptCaptureResponse>> GetRecentAsync(Guid applicationId, string promptType, string? promptVersion = null)
        {
            if (!string.Equals(webHostEnvironment.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
            {
                throw new UserFriendlyException("Prompt capture is only available in development.");
            }

            if (string.IsNullOrWhiteSpace(promptType))
            {
                return new List<AIPromptCaptureResponse>();
            }

            await EnsurePromptCapturePermissionAsync(promptType);
            await EnsurePromptCaptureFeatureEnabledAsync(promptType);
            var captures = promptIoCaptureStore.GetRecent(applicationId.ToString(), promptType, promptVersion);
            return new List<AIPromptCaptureResponse>(captures);
        }

        private async Task EnsurePromptCapturePermissionAsync(string promptType)
        {
            var permissionName = promptType switch
            {
                AIPromptTypes.AttachmentSummary => AIPermissions.AttachmentSummary.AttachmentSummaryDefault,
                AIPromptTypes.ApplicationAnalysis => AIPermissions.ApplicationAnalysis.ApplicationAnalysisDefault,
                AIPromptTypes.ScoresheetSection => AIPermissions.ScoringAssistant.ScoringAssistantDefault,
                _ => null
            };

            if (string.IsNullOrWhiteSpace(permissionName))
            {
                throw new UserFriendlyException("Unknown prompt type.");
            }

            if (!await permissionChecker.IsGrantedAsync(permissionName))
            {
                throw new AbpAuthorizationException("The user doesn't have permission to view prompt capture for this prompt type.");
            }
        }

        private async Task EnsurePromptCaptureFeatureEnabledAsync(string promptType)
        {
            var featureName = promptType switch
            {
                AIPromptTypes.AttachmentSummary => "Unity.AI.AttachmentSummaries",
                AIPromptTypes.ApplicationAnalysis => "Unity.AI.ApplicationAnalysis",
                AIPromptTypes.ScoresheetSection => "Unity.AI.Scoring",
                _ => null
            };

            if (string.IsNullOrWhiteSpace(featureName))
            {
                throw new UserFriendlyException("Unknown prompt type.");
            }

            if (!await featureChecker.IsEnabledAsync(featureName))
            {
                throw new UserFriendlyException("Prompt capture is not enabled for this prompt type.");
            }
        }
    }
}
