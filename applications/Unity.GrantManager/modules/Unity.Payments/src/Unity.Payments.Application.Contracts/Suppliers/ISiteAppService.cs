using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Payments.Suppliers
{
    public interface ISiteAppService : IApplicationService
    {
        Task<SiteDto> GetAsync(Guid id);
    }
}
