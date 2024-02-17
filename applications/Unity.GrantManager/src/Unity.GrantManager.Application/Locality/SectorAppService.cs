using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.GrantManager.Cache;
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

        private readonly ISubSectorRepository _subSectorRepository;
        private readonly ISettingManager _settingManager;
        private readonly IDistributedCache<IList<SectorDto>, CacheKey> _cache;
        private readonly ICurrentTenant _currentTenant;

        private readonly string _filterType = "SectorFilter";

        public SectorAppService(ISectorRepository sectorRepository,
            ISubSectorRepository subSectorRepository,
            ISettingManager settingManager,
            ICurrentTenant currentTenant,
            IDistributedCache<IList<SectorDto>, CacheKey> cache)
        {
            _sectorRepository = sectorRepository;
            _subSectorRepository = subSectorRepository;
            _settingManager = settingManager;
            _currentTenant = currentTenant;
            _cache = cache;
        }

        public async Task<IList<SectorDto>> GetListAsync()
        {
            var cacheKey = new CacheKey { CacheType = _filterType, TenantGuid = _currentTenant.GetId() };
            return await _cache.GetOrAddAsync(
                cacheKey,
                async () => await GetTenantSectors()
            ) ?? new List<SectorDto>();
        }

        public async Task<IList<SectorDto>> GetTenantSectors()
        { 
            var sectorsQueryable = await _sectorRepository.GetQueryableAsync();

            var query = from sector in sectorsQueryable
                        join subsector in await _subSectorRepository.GetQueryableAsync()
                            on sector.Id equals subsector.SectorId into subSectors
                        orderby sector.SectorName
                        select new { sector, subSectors, sector.SectorCode };

            var queryResult = await AsyncExecuter.ToListAsync(query);

            var applicationSectorDtos = queryResult.Select(x =>
            {
                var sector = ObjectMapper.Map<Sector, SectorDto>(x.sector);
                sector.SubSectors = ObjectMapper.Map<List<SubSector>, List<SubSectorDto>>(x.subSectors.OrderBy(ss => ss.SubSectorName).ToList());
                return sector;
            }).ToList();

            var sectorFilter = await _settingManager.GetOrNullForCurrentTenantAsync(_filterType);
            if (string.IsNullOrEmpty(sectorFilter))
            {
                return applicationSectorDtos;
            }
            else
            {
                string[] sectorCodes = sectorFilter.Split(',');
                return applicationSectorDtos.Where(x => sectorCodes.Contains(x.SectorCode)).ToList();
            }
        }
    }
}
