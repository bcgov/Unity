using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications
{
    public interface IApplicationAIScoringAppService : IApplicationService
    {
        Task<string> GenerateAIScoresheetAnswersAsync(Guid applicationId);
    }
}
