using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Unity.Payments.Enums;

namespace Unity.Payments.Suppliers
{
    public interface ISupplierAppService : IApplicationService
    {
        Task<SupplierDto?> GetAsync(Guid id);
        Task<SupplierDto?> GetByCorrelationAsync(GetSupplierByCorrelationDto requestDto);
        Task<SupplierDto?> GetBySupplierNumberAsync(string? supplierNumber);
        Task<SupplierDto> CreateAsync(CreateSupplierDto createSupplierDto);
        Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierDto updateSupplierDto);
        Task<SiteDto> CreateSiteAsync(Guid id, CreateSiteDto createSiteDto);
        Task<SiteDto> UpdateSiteAsync(Guid id, Guid siteId, UpdateSiteDto updateSiteDto);
        Task<dynamic> GetSitesBySupplierNumberAsync(string? supplierNumber, Guid applicantId, Guid? applicationId = null);
        Task DeleteAsync(Guid id);
        SiteDto GetSiteDtoFromSiteEto(SiteEto siteEto, Guid supplierId, PaymentGroup? defaultPaymentGroup = null);
    }
}
