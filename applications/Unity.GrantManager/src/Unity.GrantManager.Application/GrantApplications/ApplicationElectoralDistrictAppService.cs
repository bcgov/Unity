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
    [ExposeServices(typeof(ApplicationElectoralDistrictAppService), typeof(IApplicationElectoralDistrictService))]
    public class ApplicationElectoralDistrictAppService : ApplicationService, IApplicationElectoralDistrictService
    {
        private readonly IApplicationElectoralDistrictRepository _applicationElectoralDistrictRepository;
        public ApplicationElectoralDistrictAppService(IApplicationElectoralDistrictRepository applicationElectoralDistrictRepository)
        {
            _applicationElectoralDistrictRepository = applicationElectoralDistrictRepository;
        }

        public async Task<IList<ApplicationElectoralDistrictDto>> GetListAsync()
        {
            var electoralDistricts = await _applicationElectoralDistrictRepository.GetListAsync();

            return ObjectMapper.Map<List<ApplicationElectoralDistrict>, List<ApplicationElectoralDistrictDto>>(electoralDistricts.OrderBy(s => s.ElectoralDistrictCode).ToList());
        }
    }
}
