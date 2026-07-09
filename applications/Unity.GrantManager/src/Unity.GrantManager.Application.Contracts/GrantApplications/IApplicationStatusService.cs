using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationStatusService : IApplicationService
{
    Task<IList<ApplicationStatusDto>> GetListAsync();
    Task<IList<ApplicantPortalStatusDto>> GetApplicantPortalStatusListAsync();
    Task<ProgramDetailsDto> GetProgramDetailsAsync();
    Task UpdateExternalStatusLabelsAsync(UpdateApplicationStatusExternalLabelsDto input);
    Task UpdateProgramDetailsAsync(UpdateProgramDetailsDto input);
}
