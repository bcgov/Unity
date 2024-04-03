using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Payments.SupplierInfo
{
    public interface ISupplierInfoAppService : IApplicationService
    {
        Task<List<SiteDto>> GetSitesAsync(Guid applicationId);
    }
}
