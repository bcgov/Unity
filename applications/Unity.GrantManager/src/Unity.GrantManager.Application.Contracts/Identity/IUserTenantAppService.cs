using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Identity
{
    public interface IUserTenantAppService : IApplicationService
    {
        Task<UserTenantAccountDto?> GetUserAdminAccountAsync(string oidcSub);
        Task<List<UserTenantAccountDto>> GetUserTenantsAsync(string oidcSub);
    }
}
