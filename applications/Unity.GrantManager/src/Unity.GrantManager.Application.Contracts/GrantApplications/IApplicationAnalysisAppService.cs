using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications
{
    public interface IApplicationAnalysisAppService : IApplicationService
    {
        Task<string> GenerateApplicationAnalysisAsync(Guid applicationId, string? promptVersion = null);
    }
}

