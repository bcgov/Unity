using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Dashboard;

public interface IDashboardAppService : IApplicationService
{
    Task<List<GetEconomicRegionDto>> GetEconomicRegionCountAsync();
    Task<List<GetSectorDto>> GetSectorCountAsync();
    Task<List<GetApplicationStatusDto>> GetApplicationStatusCountAsync();
}
