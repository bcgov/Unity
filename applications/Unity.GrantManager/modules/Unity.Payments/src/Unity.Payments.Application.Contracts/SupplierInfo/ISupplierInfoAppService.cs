using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Payments.SupplierInfo
{
    public interface ISupplierInfoAppService : IApplicationService
    {
        Task<List<SiteDto>> GetSitesAsync(GetSitesRequestDto requestDto);
        Task InsertSiteAsync(Guid applicantId, string supplierNumber, string siteNumber, int payGroup, string? addressLine1, string? addressLine2, string? addressLine3, string? city, string? province, string? postalCode);
        Task InsertSupplierAsync(string? supplierNumber, Guid applicantId);
    }
}
