using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.AI;
using Volo.Abp;

namespace Unity.GrantManager.GrantApplications
{
    public class ApplicationAIPromptCaptureAppService(
        IAIPromptIoCaptureStore promptIoCaptureStore,
        IWebHostEnvironment webHostEnvironment)
        : GrantManagerAppService, IApplicationAIPromptCaptureAppService
    {
        public Task<List<AIPromptIoCaptureResponse>> GetRecentAsync(Guid applicationId, string promptType, string? promptVersion = null)
        {
            if (!string.Equals(webHostEnvironment.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
            {
                throw new UserFriendlyException("Prompt capture is only available in development.");
            }

            if (string.IsNullOrWhiteSpace(promptType))
            {
                return Task.FromResult(new List<AIPromptIoCaptureResponse>());
            }

            var captures = promptIoCaptureStore.GetRecent(applicationId.ToString(), promptType, promptVersion);
            return Task.FromResult(new List<AIPromptIoCaptureResponse>(captures));
        }
    }
}
