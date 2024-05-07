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
    [ExposeServices(typeof(ElectoralDistrictAppService), typeof(IElectoralDistrictService))]
    public class ElectoralDistrictAppService : ApplicationService, IElectoralDistrictService
    {
        private readonly IElectoralDistrictRepository _electoralDistrictRepository;
        private readonly IDistributedCache<IList<ElectoralDistrictDto>, LocalityCacheKey> _cache;

        public ElectoralDistrictAppService(IElectoralDistrictRepository electoralDistrictRepository,
            IDistributedCache<IList<ElectoralDistrictDto>, LocalityCacheKey> cache)
        {
            _electoralDistrictRepository = electoralDistrictRepository;
            _cache = cache;
        }

        public virtual async Task<IList<ElectoralDistrictDto>> GetListAsync()
        {
            var cacheKey = new LocalityCacheKey(SettingsConstants.ElectoralDistrictsCacheKey, null);
            return await _cache.GetOrAddAsync(
                cacheKey,
                GetElectoralDistricts
            ) ?? [];           
        }

        protected virtual async Task<IList<ElectoralDistrictDto>> GetElectoralDistricts()
        {
            var electoralDistricts = await _electoralDistrictRepository.GetListAsync();

            return ObjectMapper.Map<List<ElectoralDistrict>, List<ElectoralDistrictDto>>([.. electoralDistricts.OrderBy(s => s.ElectoralDistrictCode)]);
        }
    }
}
