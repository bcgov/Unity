using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications
{
    public interface IApplicationApplicantAppService : IApplicationService
    {
        Task<ApplicationApplicantInfoDto> GetByApplicationIdAsync(Guid applicationId);
    }
}
