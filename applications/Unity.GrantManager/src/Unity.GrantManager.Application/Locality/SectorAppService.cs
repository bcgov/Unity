using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Locality
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(SectorAppService), typeof(ISectorService))]
    public class SectorAppService : ApplicationService, ISectorService
    {
        private readonly ISectorRepository _sectorRepository;

        private readonly ISubSectorRepository _subSectorRepository;

        public SectorAppService(ISectorRepository sectorRepository,
            ISubSectorRepository subSectorRepository)
        {
            _sectorRepository = sectorRepository;
            _subSectorRepository = subSectorRepository;
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

            return applicationSectorDtos;
        }
    }
}
