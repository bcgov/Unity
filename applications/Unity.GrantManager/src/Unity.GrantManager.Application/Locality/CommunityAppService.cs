using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Settings;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Locality
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(CommunityAppService), typeof(ICommunityService))]
    public class CommunityAppService(ICommunityRepository communityRepository,
        IDistributedCache<CommunityAppService.CommunityCache, string> cache) : ApplicationService, ICommunityService
    {        
        public async Task<IList<CommunityDto>> GetListAsync()
        {
            var communitiesCache = await cache.GetOrAddAsync(
                SettingsConstants.CommunitiesCacheKey,
                GetCommunitiesAsync,
                () => new DistributedCacheEntryOptions
                {                    
                    AbsoluteExpiration = DateTimeOffset.Now.AddHours(SettingsConstants.DefaultLocalityCacheHours)
                }
            );

            return communitiesCache?.Communities ?? [];
        }

        protected virtual async Task<CommunityCache> GetCommunitiesAsync()
        {
            var communities = await communityRepository.GetListAsync();

            return new CommunityCache() { Communities = ObjectMapper.Map<List<Community>, List<CommunityDto>>([.. communities.OrderBy(c => c.Name)]) };
        }

        [IgnoreMultiTenancy]
        public class CommunityCache
        {
            public CommunityCache()
            {
                Communities = [];
            }

            public List<CommunityDto> Communities { get; set; }
        }
    }
}
