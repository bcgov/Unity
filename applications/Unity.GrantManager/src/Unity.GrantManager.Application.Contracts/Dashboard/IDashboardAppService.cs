using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Dashboard;

public interface IDashboardAppService : IApplicationService
{
    Task<List<GetEconomicRegionDto>> GetEconomicRegionCountAsync(Guid[] intakeIds, string?[] categories);
    Task<List<GetSectorDto>> GetSectorCountAsync(Guid[] intakeIds, string?[] categories);
    Task<List<GetApplicationStatusDto>> GetApplicationStatusCountAsync(Guid[] intakeIds, string?[] categories);
    Task<List<GetApplicationTagDto>> GetApplicationTagsCountAsync(Guid[] intakeIds, string?[] categories);
    Task<List<GetSubsectorRequestedAmtDto>> GetRequestedAmountPerSubsectorAsync(Guid[] intakeIds, string?[] categories);
}
