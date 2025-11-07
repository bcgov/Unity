using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Domain.Suppliers.ValueObjects;
using Volo.Abp.Features;

namespace Unity.Payments.Suppliers
{
    [RequiresFeature("Unity.Payments")]
    public class SupplierAppService(ISupplierRepository supplierRepository,
                                    ISiteAppService siteAppService) : PaymentsAppService, ISupplierAppService
    {
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

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

            var result = await supplierRepository.InsertAsync(supplier);
            return ObjectMapper.Map<Supplier, SupplierDto>(result);
        }

        public virtual async Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierDto updateSupplierDto)
        {
            var supplier = await supplierRepository.GetAsync(id);
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

            Supplier result = await supplierRepository.UpdateAsync(supplier);
            return ObjectMapper.Map<Supplier, SupplierDto>(result);
        }

        public virtual async Task<SupplierDto?> GetAsync(Guid id)
        {
            try
            {
                var result = await supplierRepository.GetAsync(id);
                if (result == null) return null;
                return ObjectMapper.Map<Supplier, SupplierDto>(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching supplier");
                return null;
            }
        }

        public virtual async Task<SupplierDto?> GetByCorrelationAsync(GetSupplierByCorrelationDto requestDto)
        {
            var result = await supplierRepository.GetByCorrelationAsync(requestDto.CorrelationId, requestDto.CorrelationProvider, requestDto.IncludeDetails);

            if (result == null) return null;

            return ObjectMapper.Map<Supplier, SupplierDto?>(result);
        }

        public virtual async Task<SupplierDto?> GetBySupplierNumberAsync(string? supplierNumber)
        {
            if (supplierNumber == null) return null;

            bool includeDetails = true;
            var result = await supplierRepository.GetBySupplierNumberAsync(supplierNumber, includeDetails);

            if (result == null) return null;

            return ObjectMapper.Map<Supplier, SupplierDto?>(result);
        }

        public async Task<List<SiteDto>> GetSitesBySupplierNumberAsync(string? supplierNumber)
        {
            var supplier = await GetBySupplierNumberAsync(supplierNumber);
            if (supplier == null) return new List<SiteDto>();
            List<Site> sites = await siteAppService.GetSitesBySupplierIdAsync(supplier.Id);
            return sites.Select(ObjectMapper.Map<Site, SiteDto>).ToList();
        }

        public virtual async Task<SiteDto> CreateSiteAsync(Guid id, CreateSiteDto createSiteDto)
        {
            var supplier = await supplierRepository.GetAsync(id, true);

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
            var supplier = await supplierRepository.GetAsync(id, true);

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

        public virtual async Task DeleteAsync(Guid id)
        {
            await supplierRepository.DeleteAsync(id);
        }
    }
}
