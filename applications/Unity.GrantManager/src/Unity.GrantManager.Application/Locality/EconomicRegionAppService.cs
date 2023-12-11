using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Locality
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(EconomicRegionAppService), typeof(IEconomicRegionService))]
    public class EconomicRegionAppService : ApplicationService, IEconomicRegionService
    {
        private readonly IEconomicRegionRepository _economicRegionRepository;
        public EconomicRegionAppService(IEconomicRegionRepository economicRegionRepository)
        {
            _economicRegionRepository = economicRegionRepository;
        }

        public async Task<IList<EconomicRegionDto>> GetListAsync()
        {
            var economicRegions = await _economicRegionRepository.GetListAsync();

            return ObjectMapper.Map<List<EconomicRegion>, List<EconomicRegionDto>>(economicRegions.OrderBy(s => s.EconomicRegionCode).ToList());
        }
    }
}
