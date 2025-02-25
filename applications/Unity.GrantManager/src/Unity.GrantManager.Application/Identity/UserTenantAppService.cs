using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Volo.Abp.Users;

namespace Unity.GrantManager.Identity
{
    public class UserTenantAppService(IUserAccountsRepository userAccountsRepository,
        ITenantRepository tenantRepository,
        ICurrentUser currentUser,
        IDataFilter dataFilter) : GrantManagerAppService, IUserTenantAppService
    {
        [RemoteService(false)]
        public async Task<List<UserTenantAccountDto>> GetUserTenantsAsync(string oidcSub)
        {
            List<UserTenantAccountDto> userTenantAccounts = [];

            using (dataFilter.Disable<IMultiTenant>())
            {
                var userAccounts = (await userAccountsRepository
                    .GetListByOidcSub(oidcSub))
                    .ToList();

                var tenants = await tenantRepository.GetListAsync();

                foreach (var tenant in tenants)
                {
                    var userAccount = userAccounts.Find(s => s.TenantId == tenant.Id);

                    if (userAccount != null)
                    {
                        userTenantAccounts.Add(new UserTenantAccountDto()
                        {
                            Id = userAccount.Id,
                            OidcSub = oidcSub.ToSubjectWithoutIdp(),
                            TenantName = tenant?.Name ?? null,
                            TenantId = userAccount.TenantId,
                            Username = userAccount.UserName,
                            DisplayName = userAccount.GetProperty("DisplayName")?.ToString() ?? null
                        });
                    }
                }
            }

            return [.. userTenantAccounts.OrderBy(s => s.TenantName)];
        }

        [RemoteService(false)]
        public async Task<UserTenantAccountDto?> GetUserAdminAccountAsync(string oidcSub)
        {
            using (dataFilter.Disable<IMultiTenant>())
            {
                var userAccounts = (await userAccountsRepository
                    .GetListByOidcSub(oidcSub))
                    .ToList();

                var adminAccount = userAccounts.Find(s => s.TenantId == null);

                if (adminAccount == null) return null;
                
                return new UserTenantAccountDto()
                {
                    Id = adminAccount.Id,
                    OidcSub = oidcSub.ToSubjectWithoutIdp(),
                    TenantName = null,
                    TenantId = adminAccount.TenantId,
                    Username = adminAccount.UserName,
                    DisplayName = adminAccount.GetProperty("DisplayName")?.ToString() ?? null
                };
            }
        }

        [Authorize]
        public async Task<List<UserTenantAccountDto>> GetListAsync()
        {
            var oidcSub = currentUser.FindClaim("preferred_username");
            if (oidcSub == null)
            {
                return [];
            }

            return await GetUserTenantsAsync(oidcSub.Value.ToSubjectWithoutIdp() ?? string.Empty);
        }
    }
}
