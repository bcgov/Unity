using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Features;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Domain.Suppliers.ValueObjects;

namespace Unity.Payments.Suppliers
{
    [RequiresFeature("Unity.Payments")]
    [Authorize]
    public class SupplierAppService : PaymentsAppService, ISupplierAppService
    {
        private readonly ISupplierRepository _supplierRepository;

        public SupplierAppService(ISupplierRepository supplierRepository)
        {
            _supplierRepository = supplierRepository;
        }

        public virtual async Task<SupplierDto> CreateAsync(CreateSupplierDto createSupplierDto)
        {
            Supplier supplier = new Supplier(Guid.NewGuid(),
                createSupplierDto.Name,
                createSupplierDto.Number,
                createSupplierDto.Subcategory,
                createSupplierDto.ProviderId,
                createSupplierDto.BusinessNumber,
                createSupplierDto.Status,
                createSupplierDto.SupplierProtected,
                createSupplierDto.StandardIndustryClassification,
                createSupplierDto.LastUpdatedInCAS,
                createSupplierDto.CorrelationId,
                createSupplierDto.CorrelationProvider,
                new MailingAddress(createSupplierDto.MailingAddress,
                    createSupplierDto.City,
                    createSupplierDto.Province,
                    createSupplierDto.PostalCode));

            var result = await _supplierRepository.InsertAsync(supplier);
            return ObjectMapper.Map<Supplier, SupplierDto>(result);
        }

        public virtual async Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierDto updateSupplierDto)
        {
            var supplier = await _supplierRepository.GetAsync(id);
            supplier.Name = updateSupplierDto.Name;
            supplier.Number = updateSupplierDto.Number;
            supplier.Subcategory = updateSupplierDto.Subcategory;
            supplier.ProviderId = updateSupplierDto.ProviderId;
            supplier.BusinessNumber = updateSupplierDto.BusinessNumber;
            supplier.Status = updateSupplierDto.Status;
            supplier.SupplierProtected = updateSupplierDto.SupplierProtected;
            supplier.StandardIndustryClassification = updateSupplierDto.StandardIndustryClassification;
            supplier.LastUpdatedInCAS = updateSupplierDto.LastUpdatedInCAS;
            supplier.CorrelationId = updateSupplierDto.CorrelationId;
            supplier.CorrelationProvider = updateSupplierDto.CorrelationProvider;

            supplier.SetAddress(updateSupplierDto.MailingAddress,
                updateSupplierDto.City,
                updateSupplierDto.Province,
                updateSupplierDto.PostalCode);

            Supplier result = await _supplierRepository.UpdateAsync(supplier);
            return ObjectMapper.Map<Supplier, SupplierDto>(result);
        }

        public virtual async Task<SupplierDto> GetAsync(Guid id)
        {
            var result = await _supplierRepository.GetAsync(id);
            return ObjectMapper.Map<Supplier, SupplierDto>(result);
        }

        public virtual async Task<SupplierDto?> GetByCorrelationAsync(GetSupplierByCorrelationDto requestDto)
        {
            var result = await _supplierRepository.GetByCorrelationAsync(requestDto.CorrelationId, requestDto.CorrelationProvider, requestDto.IncludeDetails);

            if (result == null) return null;

            return ObjectMapper.Map<Supplier, SupplierDto?>(result);
        }

        public virtual async Task<SiteDto> CreateSiteAsync(Guid id, CreateSiteDto createSiteDto)
        {
            var supplier = await _supplierRepository.GetAsync(id, true);

            var newId = Guid.NewGuid();
            var updateSupplier = supplier.AddSite(new Site(
                newId,
                createSiteDto.Number,
                createSiteDto.PaymentGroup,
                new Address(
                createSiteDto.AddressLine1,
                createSiteDto.AddressLine2,
                createSiteDto.AddressLine3,
                string.Empty,
                createSiteDto.City,
                createSiteDto.Province,
                createSiteDto.PostalCode)));

            return ObjectMapper.Map<Site, SiteDto>(updateSupplier.Sites.First(s => s.Id == newId));
        }

        public virtual async Task<SiteDto> UpdateSiteAsync(Guid id, Guid siteId, UpdateSiteDto updateSiteDto)
        {
            var supplier = await _supplierRepository.GetAsync(id, true);

            var updateSupplier = supplier.UpdateSite(siteId,
                updateSiteDto.Number,
                updateSiteDto.PaymentGroup,
                updateSiteDto.AddressLine1,
                updateSiteDto.AddressLine2,
                updateSiteDto.AddressLine3,
                updateSiteDto.City,
                updateSiteDto.Province,
                updateSiteDto.PostalCode);

            return ObjectMapper.Map<Site, SiteDto>(updateSupplier.Sites.First(s => s.Id == siteId));
        }
    }
}
