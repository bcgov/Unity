﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

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
        Task<List<SiteDto>> GetSitesBySupplierNumberAsync(string? supplierNumber);
    }
}
