using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Payments.Domain.Suppliers;
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

        public virtual async Task<Guid> InsertAsync(SiteDto siteDto)
        {
            Site site = new Site(siteDto);
            await _siteRepository.InsertAsync(site, true);
            return site.Id;
        }
        public virtual async Task<List<Site>> GetSitesBySupplierIdAsync(Guid supplierId)
        {
            return await _siteRepository.GetBySupplierAsync(supplierId);
        }

        public virtual async Task DeleteBySupplierIdAsync(Guid supplierId)
        {
            var sites = await _siteRepository.GetBySupplierAsync(supplierId);
            await _siteRepository.DeleteManyAsync(sites, true);
        }
    }
}
