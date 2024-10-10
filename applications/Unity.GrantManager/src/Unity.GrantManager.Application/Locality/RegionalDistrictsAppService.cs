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
    [ExposeServices(typeof(RegionalDistrictAppService), typeof(IRegionalDistrictService))]
    public class RegionalDistrictAppService(IRegionalDistrictRepository regionalDistricRepository,
        IDistributedCache<IList<RegionalDistrictDto>, LocalityCacheKey> cache) : ApplicationService, IRegionalDistrictService
    {
        public virtual async Task<IList<RegionalDistrictDto>> GetListAsync()
        {
            var cacheKey = new LocalityCacheKey(SettingsConstants.RegionalDistrictsCacheKey, null);
            return await cache.GetOrAddAsync(
                cacheKey,
                GetRegionalDistricts
            ) ?? [];
        }

        protected virtual async Task<IList<RegionalDistrictDto>> GetRegionalDistricts()
        {
            var regionalDistrict = await regionalDistricRepository.GetListAsync();

            return ObjectMapper.Map<List<RegionalDistrict>, List<RegionalDistrictDto>>([.. regionalDistrict.OrderBy(r => r.RegionalDistrictName)]);
        }
    }
}
