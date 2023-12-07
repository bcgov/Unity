using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationElectoralDistrictService : IApplicationService
{
    Task<IList<ApplicationElectoralDistrictDto>> GetListAsync();
}

