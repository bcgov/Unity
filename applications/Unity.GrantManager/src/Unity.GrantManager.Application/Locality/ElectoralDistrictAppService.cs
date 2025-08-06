using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Locality.BackgroundJobs;
using Unity.GrantManager.Settings;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Application.Services;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using static Unity.GrantManager.Locality.ElectoralDistrictAppService;

namespace Unity.GrantManager.Locality
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(ElectoralDistrictAppService), typeof(IElectoralDistrictService))]
    public class ElectoralDistrictAppService(IElectoralDistrictRepository electoralDistrictRepository,
        IDistributedCache<ElectoralDistrictsCache, string> cache,
        IBackgroundJobManager backgroundJobManager) : ApplicationService, IElectoralDistrictService
    {
        public virtual async Task<IList<ElectoralDistrictDto>> GetListAsync()
        {
            var electoralDistrictsCache = await cache.GetOrAddAsync(
                SettingsConstants.ElectoralDistrictsCacheKey,
                GetElectoralDistrictsAsync,
                () => new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddHours(SettingsConstants.DefaultLocalityCacheHours)
                }
            );

            return electoralDistrictsCache?.ElectoralDistricts ?? [];
        }
        
        [Authorize(IdentityConsts.ITAdminPolicyName)]
        public virtual async Task RetroFillElectoralDistricts(Guid tenantId)
        {
            await backgroundJobManager.EnqueueAsync(new RetrofillElectoralDistrictsBackgroundJobArgs() { TenantId = tenantId });
        }

        protected virtual async Task<ElectoralDistrictsCache> GetElectoralDistrictsAsync()
        {
            var electoralDistricts = await electoralDistrictRepository.GetListAsync();

            return new ElectoralDistrictsCache()
            {
                ElectoralDistricts = ObjectMapper.Map<List<ElectoralDistrict>, List<ElectoralDistrictDto>>([.. electoralDistricts.OrderBy(s => s.ElectoralDistrictCode)])
            };
        }

        [IgnoreMultiTenancy]
        public class ElectoralDistrictsCache
        {
            public ElectoralDistrictsCache()
            {
                ElectoralDistricts = [];
            }

            public List<ElectoralDistrictDto> ElectoralDistricts { get; set; }
        }
    }
}
