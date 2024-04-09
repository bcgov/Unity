using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Payments.SupplierInfo
{
    public interface ISupplierInfoAppService : IApplicationService
    {
        Task<List<SiteDto>> GetSiteListAsync(GetSitesRequestDto requestDto);
        Task InsertSiteAsync(Guid applicantId, string supplierNumber, string siteNumber, int payGroup, string? addressLine1, string? addressLine2, string? addressLine3, string? city, string? province, string? postalCode);
        Task UpdateSiteAsync(Guid siteId, Guid applicantId, string supplierNumber, string siteNumber, int payGroup, string? addressLine1, string? addressLine2, string? addressLine3, string? city, string? province, string? postalCode);
        Task InsertSupplierAsync(string? supplierNumber, Guid applicantId);
        Task<SiteDto> GetSiteAsync(Guid applicantId, string supplierNumber, Guid siteId);
    }
}
