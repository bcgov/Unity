using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
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

        public SectorAppService(ISectorRepository sectorRepository,
            ISubSectorRepository subSectorRepository,
            ISettingManager settingManager)
        {
            _sectorRepository = sectorRepository;
            _subSectorRepository = subSectorRepository;
            _settingManager = settingManager;
        }

        public async Task<IList<SectorDto>> GetListAsync()
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

            var sectorFilter = await _settingManager.GetOrNullForCurrentTenantAsync("SectorFilter");
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
