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
    [ExposeServices(typeof(ElectoralDistrictAppService), typeof(IElectoralDistrictService))]
    public class ElectoralDistrictAppService : ApplicationService, IElectoralDistrictService
    {
        private readonly IElectoralDistrictRepository _applicationElectoralDistrictRepository;
        public ElectoralDistrictAppService(IElectoralDistrictRepository applicationElectoralDistrictRepository)
        {
            _applicationElectoralDistrictRepository = applicationElectoralDistrictRepository;
        }

        public async Task<IList<ElectoralDistrictDto>> GetListAsync()
        {
            var electoralDistricts = await _applicationElectoralDistrictRepository.GetListAsync();

            return ObjectMapper.Map<List<ElectoralDistrict>, List<ElectoralDistrictDto>>(electoralDistricts.OrderBy(s => s.ElectoralDistrictCode).ToList());
        }
    }
}
