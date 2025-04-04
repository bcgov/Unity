using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Unity.Payments.Domain.Suppliers;

namespace Unity.Payments.Suppliers
{
    public interface ISiteAppService : IApplicationService
    {
        Task<SiteDto> GetAsync(Guid id);
        Task DeleteAsync(Guid id);
        Task<Guid> InsertAsync(SiteDto siteDto);
        Task<Guid> UpdateAsync(SiteDto siteDto);
        Task DeleteBySupplierIdAsync(Guid supplierId);
        Task<List<Site>> GetSitesBySupplierIdAsync(Guid supplierId);
        Task<Guid> MarkDeletedInUseAsync(Guid id);
    }
}
