using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Domain.Services;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Identity
{
    // Provisions/updates the host-side (TenantId == null) IdentityUser for a person designated as
    // ITAdministrator/ITOperations, and manages the UnityClaimsTypes.Role claim that the
    // UserClaims-table fallback (see UserClaimsRoleChecker in Unity.GrantManager.Web) reads back at
    // login. This is the only way to create a host account other than
    // IdentityProfileLoginAdminHandler.CreateAdminAccountAsync, which requires Keycloak to already
    // assert the ITAdministrator role - this manager exists so that dependency can be broken.
    public class HostIdentityAccountManager : DomainService
    {
        private readonly IdentityUserManager _identityUserManager;
        private readonly IIdentityUserRepository _identityUserRepository;
        private readonly IUserAccountsRepository _userAccountsRepository;
        private readonly ICurrentTenant _currentTenant;
        private readonly IDataFilter _dataFilter;

        public HostIdentityAccountManager(
            IdentityUserManager identityUserManager,
            IIdentityUserRepository identityUserRepository,
            IUserAccountsRepository userAccountsRepository,
            ICurrentTenant currentTenant,
            IDataFilter dataFilter)
        {
            _identityUserManager = identityUserManager;
            _identityUserRepository = identityUserRepository;
            _userAccountsRepository = userAccountsRepository;
            _currentTenant = currentTenant;
            _dataFilter = dataFilter;
        }

        public virtual async Task<IdentityUser> FindOrCreateHostAccountAsync(
            string username, string oidcSub, string? firstName, string? lastName, string? email, string displayName)
        {
            using (_currentTenant.Change(null))
            {
                var user = await ReactivateOrFindExistingHostAccountAsync(username, oidcSub, firstName, lastName)
                    ?? await CreateHostAccountAsync(username, email, firstName, lastName);

                user.SetProperty("OidcSub", oidcSub.ToSubjectWithoutIdp());
                user.SetProperty("DisplayName", displayName);
                await _identityUserRepository.UpdateAsync(user, true);

                return user;
            }
        }

        // Each IT role claim is assigned/revoked independently - ITAdministrator is documented as a
        // superset of ITOperations (see IdentityConsts.ITAdminOrITOperationsPolicyName), and
        // Keycloak's client_roles token claim is naturally multi-valued, so a person can legitimately
        // hold both at once. Idempotent: assigning a role the user already holds is a no-op.
        public virtual async Task AssignRoleClaimAsync(IdentityUser user, string roleName)
        {
            var existingRoleNames = await GetRoleClaimsAsync(user);
            if (existingRoleNames.Contains(roleName))
            {
                return;
            }

            ThrowIfFailed(await _identityUserManager.AddClaimAsync(user, new Claim(UnityClaimsTypes.Role, roleName)));
        }

        public virtual async Task RevokeRoleClaimAsync(IdentityUser user, string roleName)
        {
            var existingClaim = (await _identityUserManager.GetClaimsAsync(user))
                .FirstOrDefault(c => c.Type == UnityClaimsTypes.Role && c.Value == roleName);

            if (existingClaim != null)
            {
                ThrowIfFailed(await _identityUserManager.RemoveClaimAsync(user, existingClaim));
            }
        }

        public virtual async Task<IReadOnlyList<string>> GetRoleClaimsAsync(IdentityUser user)
        {
            return (await _identityUserManager.GetClaimsAsync(user))
                .Where(c => c.Type == UnityClaimsTypes.Role)
                .Select(c => c.Value)
                .ToList();
        }

        public virtual async Task<IList<IdentityUser>> GetHostAccountsAsync()
        {
            return await _userAccountsRepository.GetHostAccountsAsync();
        }

        public virtual async Task<IdentityUser> GetHostAccountAsync(Guid userId)
        {
            return await _identityUserRepository.GetAsync(userId);
        }

        // Mirrors UserImportAppService.ReactivateAndGetDeletedUserAsync - finds a matching host
        // account by username first (reactivating it if soft-deleted), falling back to an oidcSub
        // match among host accounts. Returns null only when neither lookup finds anything, meaning
        // the caller should create a brand new account.
        private async Task<IdentityUser?> ReactivateOrFindExistingHostAccountAsync(
            string username, string oidcSub, string? firstName, string? lastName)
        {
            using (_dataFilter.Disable<ISoftDelete>())
            {
                var identityUser = await _identityUserRepository.FindByTenantIdAndUserNameAsync(username, null);

                identityUser ??= (await _userAccountsRepository.GetListByOidcSub(oidcSub))
                    .FirstOrDefault(u => u.TenantId == null);

                if (identityUser == null)
                {
                    return null;
                }

                if (!string.IsNullOrWhiteSpace(username) &&
                    !string.Equals(identityUser.UserName, username, StringComparison.OrdinalIgnoreCase))
                {
                    await _identityUserManager.SetUserNameAsync(identityUser, username);
                }

                identityUser.Name = firstName ?? identityUser.Name;
                identityUser.Surname = lastName ?? identityUser.Surname;
                identityUser.IsDeleted = false;
                identityUser.DeleterId = null;
                identityUser.DeletionTime = null;
                await _identityUserRepository.UpdateAsync(identityUser);
                return identityUser;
            }
        }

        private async Task<IdentityUser> CreateHostAccountAsync(string username, string? email, string? firstName, string? lastName)
        {
            var user = new IdentityUser(GuidGenerator.Create(), username, email ?? "blank@example.com", _currentTenant.Id)
            {
                Name = firstName,
                Surname = lastName
            };

            if (!string.IsNullOrWhiteSpace(email))
            {
                user.SetEmailConfirmed(true);
            }

            ThrowIfFailed(await _identityUserManager.CreateAsync(user));

            return user;
        }

        private static void ThrowIfFailed(Microsoft.AspNetCore.Identity.IdentityResult result)
        {
            if (!result.Succeeded)
            {
                throw new AbpException(string.Join('\n', result.Errors.Select(e => $"{e.Code} {e.Description}")));
            }
        }
    }
}
