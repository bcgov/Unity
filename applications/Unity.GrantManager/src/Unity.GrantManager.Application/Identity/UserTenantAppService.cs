using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Volo.Abp.Users;

namespace Unity.GrantManager.Identity
{
    public class UserTenantAppService : GrantManagerAppService, IUserTenantAppService
    {
        private readonly IUserAccountsRepository _userAccountsRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IDataFilter _dataFilter;
        private readonly ICurrentUser _currentUser;

        public UserTenantAppService(IUserAccountsRepository userAccountsRepository,
            ITenantRepository tenantRepository,
            ICurrentUser currentUser,
            IDataFilter dataFilter)
        {
            _userAccountsRepository = userAccountsRepository;
            _tenantRepository = tenantRepository;
            _dataFilter = dataFilter;
            _currentUser = currentUser;
        }

        [RemoteService(false)]
        public async Task<IList<UserTenantAccountDto>> GetUserTenantsAsync(string oidcSub)
        {
            List<UserTenantAccountDto> userTenantAccounts = new();

            using (_dataFilter.Disable<IMultiTenant>())
            {
                var userAccounts = await _userAccountsRepository.GetListByOidcSub(oidcSub);

                // Still to Optimize
                var tenants = await _tenantRepository.GetListAsync();

                foreach (IdentityUser userAccount in userAccounts)
                {
                    var matchingTenant = tenants.Find(s => s.Id == userAccount.TenantId);

                    userTenantAccounts.Add(new UserTenantAccountDto()
                    {
                        Id = userAccount.Id,
                        OidcSub = oidcSub.ToSubjectWithoutIdp(),
                        TenantName = matchingTenant?.Name ?? null,
                        TenantId = userAccount.TenantId,
                        Username = userAccount.UserName,
                        DisplayName = userAccount.GetProperty("DisplayName")?.ToString() ?? null
                    });
                }
            }

            return userTenantAccounts
                .OrderBy(s => s.TenantName).ToList();
        }

        [Authorize]
        public async Task<IList<UserTenantAccountDto>> GetListAsync()
        {
            var oidcSub = _currentUser.FindClaim("preferred_username");
            if (oidcSub == null)
            {
                return new List<UserTenantAccountDto>();
            }

            return await GetUserTenantsAsync(oidcSub.Value.ToSubjectWithoutIdp() ?? string.Empty);
        }
    }
}
