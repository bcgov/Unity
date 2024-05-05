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
    public class RegionalDistrictAppService : ApplicationService, IRegionalDistrictService
    {
        private readonly IRegionalDistrictRepository _regionalDistrictRepository;
        private readonly IDistributedCache<IList<RegionalDistrictDto>, LocalityCacheKey> _cache;

        public RegionalDistrictAppService(IRegionalDistrictRepository regionalDistricRepository,
            IDistributedCache<IList<RegionalDistrictDto>, LocalityCacheKey> cache)
        {
            _regionalDistrictRepository = regionalDistricRepository;
            _cache = cache;
        }

        public virtual async Task<IList<RegionalDistrictDto>> GetListAsync()
        {
            var cacheKey = new LocalityCacheKey(SettingsConstants.RegionalDistrictsCacheKey, null);
            return await _cache.GetOrAddAsync(
                cacheKey,
                GetRegionalDistricts
            ) ?? [];
        }

        protected virtual async Task<IList<RegionalDistrictDto>> GetRegionalDistricts()
        {
            var regionalDistrict = await _regionalDistrictRepository.GetListAsync();

            return ObjectMapper.Map<List<RegionalDistrict>, List<RegionalDistrictDto>>([.. regionalDistrict.OrderBy(r => r.RegionalDistrictName)]);
        }
    }
}
