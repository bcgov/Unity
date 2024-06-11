using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.GrantManager.Caching;
using Unity.GrantManager.Settings;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Locality
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(EconomicRegionAppService), typeof(IEconomicRegionService))]
    public class EconomicRegionAppService : ApplicationService, IEconomicRegionService
    {
        private readonly IEconomicRegionRepository _economicRegionRepository;
        private readonly IDistributedCache<IList<EconomicRegionDto>, LocalityCacheKey> _cache;

        public EconomicRegionAppService(IEconomicRegionRepository economicRegionRepository,
            IDistributedCache<IList<EconomicRegionDto>, LocalityCacheKey> cache)
        {
            _economicRegionRepository = economicRegionRepository;
            _cache = cache;
        }

        public virtual async Task<IList<EconomicRegionDto>> GetListAsync()
        {
            var cacheKey = new LocalityCacheKey(SettingsConstants.EconomicRegionsCacheKey, null);
            return await _cache.GetOrAddAsync(
                cacheKey,
                GetEconomicRegions
            ) ?? [];
        }

        protected virtual async Task<IList<EconomicRegionDto>> GetEconomicRegions()
        {
            var economicRegions = await _economicRegionRepository.GetListAsync();

            return ObjectMapper.Map<List<EconomicRegion>, List<EconomicRegionDto>>([.. economicRegions.OrderBy(s => s.EconomicRegionName)]);
        }
    }
}
