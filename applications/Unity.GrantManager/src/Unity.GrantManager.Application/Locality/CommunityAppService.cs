using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Locality
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(CommunityAppService), typeof(ICommunityService))]
    public class CommunityAppService : ApplicationService, ICommunityService
    {
        private readonly ICommunityRepository _communityRepository;
        public CommunityAppService(ICommunityRepository communityRepository)
        {
            _communityRepository = communityRepository;
        }

        public async Task<IList<CommunityDto>> GetListAsync()
        {

            var communities = await _communityRepository.GetListAsync();

            return ObjectMapper.Map<List<Community>, List<CommunityDto>>(communities.OrderBy(c => c.Name).ToList());
        }
    }
}
