using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications
{
    public interface IApplicationAIAnalysisAppService : IApplicationService
    {
        Task<string> GenerateAIAnalysisAsync(Guid applicationId, string? promptVersion = null);
    }
}

