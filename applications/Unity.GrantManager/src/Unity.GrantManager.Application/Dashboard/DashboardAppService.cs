using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Dashboard;

[Authorize]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(DashboardAppService), typeof(IDashboardAppService))]
public class DashboardAppService : ApplicationService, IDashboardAppService
{

    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicationStatusRepository _applicationStatusRepository;

    public DashboardAppService(IRepository<Application, Guid> repository,
        IApplicationRepository applicationRepository,
        IApplicationStatusRepository applicationStatusRepository
        )
         : base()
    {
        _applicationRepository = applicationRepository;
        _applicationStatusRepository = applicationStatusRepository;
    }

    


    public async Task<List<GetEconomicRegionDto>> GetEconomicRegionCountAsync()
    {
        var query = await _applicationRepository.GetQueryableAsync();

        var result = query?.GroupBy(app => app.EconomicRegion).Select(group => new GetEconomicRegionDto { EconomicRegion = string.IsNullOrEmpty(group.Key) ? "None" : group.Key, Count = group.Count() }).OrderBy(o => o.EconomicRegion);
        if (result == null) return new List<GetEconomicRegionDto>();
        var queryResult = await AsyncExecuter.ToListAsync(result);
        return queryResult;
    }

    public async Task<List<GetSectorDto>> GetSectorCountAsync()
    {
        var query = await _applicationRepository.GetQueryableAsync();

        var result = query?.GroupBy(app => app.Sector).Select(group => new GetSectorDto { Sector = string.IsNullOrEmpty(group.Key) ? "None" : group.Key, Count = group.Count() }).OrderBy(o => o.Sector);
        if (result == null) return new List<GetSectorDto>();
        var queryResult = await AsyncExecuter.ToListAsync(result);
        return queryResult;
    }

    public async Task<List<GetApplicationStatusDto>> GetApplicationStatusCountAsync()
    {
        var query = from application in await _applicationRepository.GetQueryableAsync()
                    join appStatus in await _applicationStatusRepository.GetQueryableAsync() on application.ApplicationStatusId equals appStatus.Id
                    select new {application,appStatus};
        var result = query?.GroupBy(app => app.appStatus.InternalStatus).Select(group => new GetApplicationStatusDto { ApplicationStatus = string.IsNullOrEmpty(group.Key) ? "None" : group.Key, Count = group.Count() }).OrderBy(o => o.ApplicationStatus);
        if (result == null) return new List<GetApplicationStatusDto>();
        var queryResult = await AsyncExecuter.ToListAsync(result);
        return queryResult;
    }

}
