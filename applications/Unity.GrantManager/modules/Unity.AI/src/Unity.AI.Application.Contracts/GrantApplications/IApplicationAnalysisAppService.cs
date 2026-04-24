using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications
{
    public interface IApplicationAnalysisAppService : IApplicationService
    {
        Task<ApplicationAnalysisResultDto> GenerateApplicationAnalysisAsync(Guid applicationId, string? promptVersion = null);
        Task<ApplicationAnalysisResultDto> GenerateApplicationAnalysisForPipelineAsync(Guid applicationId, string? promptVersion = null);
    }
}
