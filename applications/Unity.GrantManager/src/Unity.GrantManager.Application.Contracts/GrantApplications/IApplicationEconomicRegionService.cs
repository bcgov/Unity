using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationEconomicRegionService : IApplicationService
{
    Task<IList<ApplicationEconomicRegionDto>> GetListAsync();
}

