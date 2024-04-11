using System;
using System.Threading.Tasks;
using Unity.Payments.Enums;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace Unity.Payments.Suppliers
{
    public class SupplierAppService : PaymentsAppService, ISupplierAppService
    {
        private readonly ISupplierRepository _supplierRepository;

        public SupplierAppService(ISupplierRepository supplierRepository)            
        {
            _supplierRepository = supplierRepository;            
        }

        public async Task<SupplierDto> CreateAsync(CreateSupplierDto createSupplierDto)
        {
            var result = await _supplierRepository.InsertAsync(new Supplier(Guid.NewGuid(),
                createSupplierDto.Name,
                createSupplierDto.Number,
                createSupplierDto.CorrelationId,
                createSupplierDto.CorrelationProvider,
                createSupplierDto.MailingAddress,
                createSupplierDto.City,
                createSupplierDto.Province,
                createSupplierDto.PostalCode));

            return ObjectMapper.Map<Supplier, SupplierDto>(result);
        }


        public async Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierDto updateSupplierDto)
        {
            var supplier = await _supplierRepository.GetAsync(id);

            supplier.SetName(updateSupplierDto.Name);
            supplier.SetNumber(updateSupplierDto.Number);
            supplier.SetAddress(updateSupplierDto.MailingAddress,
                updateSupplierDto.City,
                updateSupplierDto.Province,
                updateSupplierDto.PostalCode);

            return ObjectMapper.Map<Supplier, SupplierDto>(supplier);
        }

        public async Task<SupplierDto> GetAsync(Guid id)
        {
            var result = await _supplierRepository.GetAsync(id);
            return ObjectMapper.Map<Supplier, SupplierDto>(result);
        }

        public async Task<SupplierDto?> GetByCorrelationAsync(GetSupplierByCorrelationDto requestDto)
        {
            var result = await _supplierRepository.GetByCorrelationAsync(requestDto.CorrelationId, requestDto.CorrelationProvider, requestDto.IncludeDetails);

            if (result == null) return null;

            return ObjectMapper.Map<Supplier, SupplierDto?>(result);
        }

        public async Task<SiteDto> CreateSiteAsync(Guid id, CreateSiteDto createSiteDto)
        {
            var supplier = await _supplierRepository.GetAsync(id, true);

            var newId = Guid.NewGuid();
            var updateSupplier = supplier.AddSite(new Site(
                newId,
                createSiteDto.Number,
                (PaymentGroup)createSiteDto.PaymentGroup,
                createSiteDto.AddressLine1,
                createSiteDto.AddressLine2,
                createSiteDto.AddressLine3,
                createSiteDto.City,
                createSiteDto.Province,
                createSiteDto.PostalCode));

            return ObjectMapper.Map<Site, SiteDto>(updateSupplier.Sites.First(s => s.Id == newId));
        }

        public async Task<SiteDto> UpdateSiteAsync(Guid id, Guid siteId, UpdateSiteDto updateSiteDto)
        {
            var supplier = await _supplierRepository.GetAsync(id, true);

            var updateSupplier = supplier.UpdateSite(siteId,
                updateSiteDto.Number,
                (PaymentGroup)updateSiteDto.PaymentGroup,
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
