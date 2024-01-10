using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.TenantManagement;

namespace Unity.GrantManager.Identity
{
    public interface IUserTenantsAppService
    {
        Task<IList<TenantDto>> GetUserTenantsAsync();
    }
}
