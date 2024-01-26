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
    [ExposeServices(typeof(RegionalDistrictAppService), typeof(IRegionalDistrictService))]
    public class RegionalDistrictAppService : ApplicationService, IRegionalDistrictService
    {
        private readonly IRegionalDistrictRepository _regionalDistrictRepository;
        public RegionalDistrictAppService(IRegionalDistrictRepository  regionalDistricRepository)
        {
            _regionalDistrictRepository = regionalDistricRepository;
        }

        public async Task<IList<RegionalDistrictDto>> GetListAsync()
        {
            var regionalDistrict = await _regionalDistrictRepository.GetListAsync();

            return ObjectMapper.Map<List<RegionalDistrict>, List<RegionalDistrictDto>>(regionalDistrict.OrderBy(r => r.RegionalDistrictName).ToList());
        }
    }
}
