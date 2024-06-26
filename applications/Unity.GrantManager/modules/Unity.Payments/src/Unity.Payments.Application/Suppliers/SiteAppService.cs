using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Domain.Suppliers.ValueObjects;
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
                new Address(
                    siteDto.AddressLine1,
                    siteDto.AddressLine2,
                    siteDto.AddressLine3,
                    siteDto.Country,
                    siteDto.City,
                    siteDto.Province,
                    siteDto.PostalCode),
                siteDto.SupplierId,
                siteDto.LastUpdatedInCas);

            await _siteRepository.InsertAsync(site);
        }

        public virtual async Task DeleteBySupplierIdAsync(Guid supplierId)
        {
            var sites = await _siteRepository.GetBySupplierAsync(supplierId);
            await _siteRepository.DeleteManyAsync(sites.Select(s => s.Id));
        }
    }
}
