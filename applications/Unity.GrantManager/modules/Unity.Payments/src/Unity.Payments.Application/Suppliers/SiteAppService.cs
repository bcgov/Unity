using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Volo.Abp.Features;

namespace Unity.Payments.Suppliers
{
    [RequiresFeature("Unity.Payments")]
    [Authorize]
    public class SiteAppService : PaymentsAppService, ISiteAppService
    {
        private readonly ISiteRepository _siteRepository;

        public SiteAppService(ISiteRepository siteRepository)
        {
            _siteRepository = siteRepository;
        }

        public virtual async Task<SiteDto> GetAsync(Guid id)
        {
            return ObjectMapper.Map<Site, SiteDto>(await _siteRepository.GetAsync(id));
        }
    }
}
