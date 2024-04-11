using System;
using System.Threading.Tasks;

namespace Unity.Payments.Suppliers
{
    public class SiteAppService : PaymentsAppService, ISiteAppService
    {
        private readonly ISiteRepository _siteRepository;

        public SiteAppService(ISiteRepository siteRepository)
        {
            _siteRepository = siteRepository;
        }

        public async Task<SiteDto> GetAsync(Guid id)
        {
            return ObjectMapper.Map<Site, SiteDto>(await _siteRepository.GetAsync(id));
        }
    }
}
