using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Locale
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(SectorAppService), typeof(ISectorService))]
    public class SectorAppService : ApplicationService, ISectorService
    {
        private readonly ISectorRepository _applicationSectorRepository;

        private readonly IApplicationSubSectorRepository _applicationSubSectorRepository;

        public SectorAppService(ISectorRepository applicationSectorRepository,
            IApplicationSubSectorRepository applicationSubSectorRepository)
        {
            _applicationSectorRepository = applicationSectorRepository;
            _applicationSubSectorRepository = applicationSubSectorRepository;
        }

        public async Task<IList<SectorDto>> GetListAsync()
        {
            var sectorsQueryable = await _applicationSectorRepository.GetQueryableAsync();

            var query = from sector in sectorsQueryable
                        join subsector in await _applicationSubSectorRepository.GetQueryableAsync()
                            on sector.Id equals subsector.SectorId into subSectors
                        select new { sector, subSectors, sector.SectorCode };

            var queryResult = await AsyncExecuter.ToListAsync(query);

            var applicationSectorDtos = queryResult.Select(x =>
            {
                var sector = ObjectMapper.Map<Sector, SectorDto>(x.sector);
                sector.SubSectors = ObjectMapper.Map<List<SubSector>, List<SubSectorDto>>((List<SubSector>)x.subSectors);
                return sector;
            }).ToList();

            return applicationSectorDtos;
        }
    }
}
