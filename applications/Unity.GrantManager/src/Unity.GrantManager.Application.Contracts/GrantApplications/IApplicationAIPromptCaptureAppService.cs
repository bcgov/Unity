using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.AI;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications
{
    public interface IApplicationAIPromptCaptureAppService : IApplicationService
    {
        Task<List<AIPromptIoCaptureResponse>> GetRecentAsync(Guid applicationId, string promptType, string? promptVersion = null);
    }
}
