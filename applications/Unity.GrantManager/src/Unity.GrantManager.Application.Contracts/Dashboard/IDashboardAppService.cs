using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Dashboard;

public interface IDashboardAppService : IApplicationService
{
    Task<List<GetEconomicRegionDto>> GetEconomicRegionCountAsync(DashboardParametersDto dashboardParams);
    Task<List<GetApplicationStatusDto>> GetApplicationStatusCountAsync(DashboardParametersDto dashboardParams);
    Task<List<GetApplicationTagDto>> GetApplicationTagsCountAsync(DashboardParametersDto dashboardParams);
    Task<List<GetSubsectorRequestedAmtDto>> GetRequestedAmountPerSubsectorAsync(DashboardParametersDto dashboardParams);
    Task<List<GetApplicationAssigneeDto>> GetApplicationAssigneeCountAsync(DashboardParametersDto dashboardParams);
    Task<List<GetRequestedApprovedAmtDto>> GetRequestApprovedCountAsync(DashboardParametersDto dashboardParams);
}
