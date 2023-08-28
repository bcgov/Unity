using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationStatusService : IApplicationService
{
    Task<IList<ApplicationStatusDto>> GetListAsync();
}
