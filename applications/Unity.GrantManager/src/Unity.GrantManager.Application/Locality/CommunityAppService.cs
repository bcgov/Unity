using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Caching;
using Unity.GrantManager.Settings;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Locality
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(CommunityAppService), typeof(ICommunityService))]
    public class CommunityAppService : ApplicationService, ICommunityService
    {
        private readonly ICommunityRepository _communityRepository;
        private readonly IDistributedCache<IList<CommunityDto>, LocalityCacheKey> _cache;

        public CommunityAppService(ICommunityRepository communityRepository,
            IDistributedCache<IList<CommunityDto>, LocalityCacheKey> cache)
        {
            _communityRepository = communityRepository;
            _cache = cache;
        }

        public async Task<IList<CommunityDto>> GetListAsync()
        {
            var cacheKey = new LocalityCacheKey(SettingsConstants.CommunitiesCacheKey, null);
            return await _cache.GetOrAddAsync(
                cacheKey,
                GetCommunities
            ) ?? [];
        }

        protected virtual async Task<IList<CommunityDto>> GetCommunities()
        {
            var communities = await _communityRepository.GetListAsync();

            return ObjectMapper.Map<List<Community>, List<CommunityDto>>([.. communities.OrderBy(c => c.Name)]);
        }
    }
}
