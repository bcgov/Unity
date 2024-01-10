using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.TenantManagement;
using Volo.Abp.Identity;

namespace Unity.GrantManager.Identity
{
    public class UserTenantsAppService : IUserTenantsAppService
    {
        // private readonly IIdentityUserRepository _identityUserRepository;
        // private readonly IdentityUserManager _uderRe

        public UserTenantsAppService(IIdentityUserRepository identityUserRepository)
        {
            // _identityUserRepository = identityUserRepository;
        }

        public async Task<IList<TenantDto>> GetUserTenantsAsync()
        {
            //_identityUserRepository.FindByTenantIdAndUserNameAsync
            return new List<TenantDto>();
        }
    }
}
