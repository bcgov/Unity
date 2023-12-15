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
    [ExposeServices(typeof(CensusSubdivisionAppService), typeof(ICensusSubdivisionService))]
    public class CensusSubdivisionAppService : ApplicationService, ICensusSubdivisionService
    {
        private readonly ICensusSubdivisionRepository _censusSubdivisionRepository;
        public CensusSubdivisionAppService(ICensusSubdivisionRepository censusSubdivisionRepository)
        {
            _censusSubdivisionRepository = censusSubdivisionRepository;
        }

        public async Task<IList<CensusSubdivisionDto>> GetListAsync()
        {

            var censusSubdivision = await _censusSubdivisionRepository.GetListAsync();

            return ObjectMapper.Map<List<CensusSubdivision>, List<CensusSubdivisionDto>>(censusSubdivision.OrderBy(c => c.CensusSubdivisionName).ToList());
        }
    }
}
