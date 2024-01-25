using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Identity
{
    public interface IUserTenantAppService : IApplicationService
    {
        Task<IList<UserTenantAccountDto>> GetUserTenantsAsync(string oidcSub);
    }
}
