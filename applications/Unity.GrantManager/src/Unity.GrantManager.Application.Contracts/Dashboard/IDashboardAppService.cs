using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Dashboard;

public interface IDashboardAppService : IApplicationService
{
    Task<List<GetEconomicRegionDto>> GetEconomicRegionCountAsync(Guid intakeId, string category);
    Task<List<GetSectorDto>> GetSectorCountAsync(Guid intakeId, string category);
    Task<List<GetApplicationStatusDto>> GetApplicationStatusCountAsync(Guid intakeId, string category);
}
