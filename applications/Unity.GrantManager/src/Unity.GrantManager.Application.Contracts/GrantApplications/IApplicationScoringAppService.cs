using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications
{
    public interface IApplicationScoringAppService : IApplicationService
    {
        Task<string> GenerateApplicationScoringAsync(Guid applicationId, string? promptVersion = null);
    }
}

