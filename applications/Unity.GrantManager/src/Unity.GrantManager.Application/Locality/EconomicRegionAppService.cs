using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Unity.GrantManager.Settings;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using static Unity.GrantManager.Locality.EconomicRegionAppService;

namespace Unity.GrantManager.Locality
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(EconomicRegionAppService), typeof(IEconomicRegionService))]
    public class EconomicRegionAppService(IEconomicRegionRepository economicRegionRepository,
        IDistributedCache<EconomicRegionCache, string> cache) : ApplicationService, IEconomicRegionService
    {
        public virtual async Task<IList<EconomicRegionDto>> GetListAsync()
        {
            var economicRegionsCache = await cache.GetOrAddAsync(
                SettingsConstants.EconomicRegionsCacheKey,
                GetEconomicRegionsAsync,
                () => new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddHours(SettingsConstants.DefaultLocalityCacheHours)
                }
            );

            return economicRegionsCache?.EconomicRegions ?? [];
        }

        protected virtual async Task<EconomicRegionCache> GetEconomicRegionsAsync()
        {
            var economicRegions = await economicRegionRepository.GetListAsync();

            return new EconomicRegionCache()
            {
                EconomicRegions = ObjectMapper.Map<List<EconomicRegion>, List<EconomicRegionDto>>([.. economicRegions.OrderBy(s => s.EconomicRegionName)])
            };
        }

        [IgnoreMultiTenancy]
        public class EconomicRegionCache
        {
            public EconomicRegionCache()
            {
                EconomicRegions = [];
            }

            public List<EconomicRegionDto> EconomicRegions { get; set; }
        }
    }
}
