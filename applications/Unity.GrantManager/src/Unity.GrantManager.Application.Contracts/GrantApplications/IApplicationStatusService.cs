using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationStatusService : IApplicationService
{
    Task<IList<ApplicationStatusDto>> GetListAsync();
    Task<IList<ApplicantPortalStatusDto>> GetApplicantPortalStatusListAsync();
    Task<ApplicantPortalProgramDetailsDto> GetApplicantPortalProgramDetailsAsync();
    Task UpdateExternalStatusLabelsAsync(UpdateApplicationStatusExternalLabelsDto input);
    Task UpdateApplicantPortalProgramDetailsAsync(UpdateApplicantPortalProgramDetailsDto input);
}
