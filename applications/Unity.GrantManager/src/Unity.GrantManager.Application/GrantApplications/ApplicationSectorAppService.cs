using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.GrantApplications
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(ApplicationSectorAppService), typeof(IApplicationSectorService))]
    public class ApplicationSectorAppService : ApplicationService, IApplicationSectorService
    {
        private readonly IApplicationSectorRepository _applicationSectorRepository;

        private readonly IApplicationSubSectorRepository _applicationSubSectorRepository;

        public ApplicationSectorAppService(IApplicationSectorRepository applicationSectorRepository,
            IApplicationSubSectorRepository applicationSubSectorRepository)
        {
            _applicationSectorRepository = applicationSectorRepository;
            _applicationSubSectorRepository = applicationSubSectorRepository;
        }

        public async Task<IList<ApplicationSectorDto>> GetListAsync()
        {
            var sectorsQueryable = await _applicationSectorRepository.GetQueryableAsync();

            var query = from sector in sectorsQueryable
                        join subsector in await _applicationSubSectorRepository.GetQueryableAsync() 
                            on sector.Id equals subsector.SectorId into subSectors
                        select new { sector, subSectors, sector.SectorCode };

            var queryResult = await AsyncExecuter.ToListAsync(query);

            var applicationSectorDtos = queryResult.Select(x =>
            {
                var sector = ObjectMapper.Map<ApplicationSector, ApplicationSectorDto>(x.sector);
                sector.SubSectors = ObjectMapper.Map<List<ApplicationSubSector>, List<ApplicationSubSectorDto>>((List<ApplicationSubSector>)x.subSectors);
                return sector;
            }).ToList();

            return applicationSectorDtos;
        }
    }
}
