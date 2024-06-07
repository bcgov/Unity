using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public virtual async Task InsertAsync(SiteDto siteDto)
        {
            Site site = new Site(
                siteDto.Number,
                siteDto.PaymentGroup,
                siteDto.EmailAddress,
                siteDto.EFTAdvicePref,
                siteDto.ProviderId,
                siteDto.Status,
                siteDto.SiteProtected,
                siteDto.AddressLine1,
                siteDto.AddressLine2,
                siteDto.AddressLine3,
                siteDto.Country,
                siteDto.City,
                siteDto.Province,
                siteDto.PostalCode,
                siteDto.SupplierId,
                siteDto.LastUpdatedInCas);

            await _siteRepository.InsertAsync(site);
        }

        public virtual async Task DeleteBySupplierIdAsync(Guid supplierId)
        {
            IQueryable<Site> queryableAssignment = _siteRepository.GetQueryableAsync().Result;
            List<Site> sites = queryableAssignment
                .Where(a => a.SupplierId.Equals(supplierId)).ToList();

            foreach (Site site in sites)
            {
                await _siteRepository.DeleteAsync(site);
            }            
        }
    }
}
