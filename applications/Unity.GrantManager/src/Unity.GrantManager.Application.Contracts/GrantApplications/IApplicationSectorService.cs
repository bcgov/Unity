using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationSectorService : IApplicationService
{
    Task<IList<ApplicationSectorDto>> GetListAsync();
}

public interface IApplicationSubSectorService : IApplicationService
{
    Task<IList<ApplicationSectorDto>> GetListAsync();
}
