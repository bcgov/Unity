using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.GrantManager.Caching;
using Unity.GrantManager.Settings;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;

namespace Unity.GrantManager.Locality
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(SectorAppService), typeof(ISectorService))]
    public class SectorAppService : ApplicationService, ISectorService
    {
        private readonly ISectorRepository _sectorRepository;
        private readonly ISettingManager _settingManager;
        private readonly IDistributedCache<IList<SectorDto>, LocalityCacheKey> _cache;
        private readonly ICurrentTenant _currentTenant;

        public SectorAppService(ISectorRepository sectorRepository,
            ISettingManager settingManager,
            ICurrentTenant currentTenant,
            IDistributedCache<IList<SectorDto>, LocalityCacheKey> cache)
        {
            _sectorRepository = sectorRepository;
            _settingManager = settingManager;
            _currentTenant = currentTenant;
            _cache = cache;
        }

        public virtual async Task<IList<SectorDto>> GetListAsync()
        {
            var cacheKey = new LocalityCacheKey(SettingsConstants.SectorFilterName, _currentTenant.GetId());
            return await _cache.GetOrAddAsync(
                cacheKey,
                GetTenantSectors
            ) ?? [];
        }

        protected virtual async Task<IList<SectorDto>> GetTenantSectors()
        {
            var sectors = await _sectorRepository.GetListAsync(true);

            var applicationSectorDtos = sectors.Select(x =>
            {
                var sector = ObjectMapper.Map<Sector, SectorDto>(x);
                sector.SubSectors = ObjectMapper.Map<List<SubSector>, List<SubSectorDto>>([.. x.SubSectors.OrderBy(ss => ss.SubSectorName)]);
                return sector;
            }).ToList();

            var sectorFilter = await _settingManager.GetOrNullForCurrentTenantAsync(SettingsConstants.SectorFilterName);
            if (string.IsNullOrEmpty(sectorFilter))
            {
                return [.. applicationSectorDtos.OrderBy(s => s.SectorName)];
            }
            else
            {
                string[] sectorCodes = sectorFilter.Split(',');
                return [.. applicationSectorDtos.Where(x => sectorCodes.Contains(x.SectorCode)).OrderBy(s => s.SectorName)];
            }
        }
    }
}
