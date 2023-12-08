using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.GrantApplications
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(ApplicationEconomicRegionAppService), typeof(IApplicationEconomicRegionService))]
    public class ApplicationEconomicRegionAppService : ApplicationService, IApplicationEconomicRegionService
    {
        private readonly IApplicationEconomicRegionRepository _applicationEconomicRegionRepository;
        public ApplicationEconomicRegionAppService(IApplicationEconomicRegionRepository applicationEconomicRegionRepository)
        {
            _applicationEconomicRegionRepository = applicationEconomicRegionRepository;
        }

        public async Task<IList<ApplicationEconomicRegionDto>> GetListAsync()
        {
            var economicRegions = await _applicationEconomicRegionRepository.GetListAsync();

            return ObjectMapper.Map<List<ApplicationEconomicRegion>, List<ApplicationEconomicRegionDto>>(economicRegions.OrderBy(s => s.EconomicRegionCode).ToList());
        }
    }
}
