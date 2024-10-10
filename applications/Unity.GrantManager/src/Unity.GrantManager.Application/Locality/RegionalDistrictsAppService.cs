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

namespace Unity.GrantManager.Locality
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(RegionalDistrictAppService), typeof(IRegionalDistrictService))]
    public class RegionalDistrictAppService(IRegionalDistrictRepository regionalDistricRepository,
        IDistributedCache<RegionalDistrictsCache, string> cache) : ApplicationService, IRegionalDistrictService
    {
        public virtual async Task<IList<RegionalDistrictDto>> GetListAsync()
        {
            var regionalDistrictsCache = await cache.GetOrAddAsync(
                SettingsConstants.RegionalDistrictsCacheKey,
                GetRegionalDistrictsAsync,
                () => new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddHours(SettingsConstants.DefaultLocalityCacheHours)
                }
            );

            return regionalDistrictsCache?.RegionalDistricts ?? [];
        }

        protected virtual async Task<RegionalDistrictsCache> GetRegionalDistrictsAsync()
        {
            var regionalDistrict = await regionalDistricRepository.GetListAsync();

            return new RegionalDistrictsCache()
            {
                RegionalDistricts = ObjectMapper.Map<List<RegionalDistrict>, List<RegionalDistrictDto>>([.. regionalDistrict.OrderBy(r => r.RegionalDistrictName)])
            };
        }
    }

    [IgnoreMultiTenancy]
    public class RegionalDistrictsCache
    {
        public RegionalDistrictsCache()
        {
            RegionalDistricts = [];
        }

        public List<RegionalDistrictDto> RegionalDistricts { get; set; }
    }
}

