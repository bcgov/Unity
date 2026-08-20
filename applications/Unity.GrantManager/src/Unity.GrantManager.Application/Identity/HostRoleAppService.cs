using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Integrations.Css;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Data;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Identity
{
    // Lets an ITAdmin designate a person as ITAdministrator/ITOperations purely from app data,
    // independent of Keycloak. See HostIdentityAccountManager for why this needs to find-or-create
    // a host (TenantId == null) account rather than just stamping a claim on an existing one.
    [Authorize(IdentityConsts.ITAdminPolicyName)]
    public class HostRoleAppService : GrantManagerAppService, IHostRoleAppService
    {
        private static readonly string[] AssignableRoleNames =
        [
            IdentityConsts.ITAdminRoleName,
            IdentityConsts.ITOperationsRoleName
        ];

        private readonly ICssUsersApiService _cssUsersApiService;
        private readonly HostIdentityAccountManager _hostIdentityAccountManager;

        public HostRoleAppService(
            ICssUsersApiService cssUsersApiService,
            HostIdentityAccountManager hostIdentityAccountManager)
        {
            _cssUsersApiService = cssUsersApiService;
            _hostIdentityAccountManager = hostIdentityAccountManager;
        }

        // One row per user - each row carries every IT role that user currently holds (0 to 2).
        // Host accounts with no IT role claims left are omitted entirely, so revoking someone's
        // last role drops them from the list rather than leaving an empty row behind.
        public async Task<List<HostRoleAssignmentDto>> GetListAsync()
        {
            var hostAccounts = await _hostIdentityAccountManager.GetHostAccountsAsync();
            var assignments = new List<HostRoleAssignmentDto>();

            foreach (var user in hostAccounts)
            {
                var roleNames = (await _hostIdentityAccountManager.GetRoleClaimsAsync(user))
                    .Where(AssignableRoleNames.Contains)
                    .ToList();

                if (roleNames.Count == 0)
                {
                    continue;
                }

                assignments.Add(new HostRoleAssignmentDto
                {
                    Id = user.Id,
                    Username = user.UserName,
                    DisplayName = user.GetProperty("DisplayName")?.ToString(),
                    Email = user.Email,
                    RoleNames = roleNames
                });
            }

            return [.. assignments.OrderBy(a => a.DisplayName)];
        }

        // Assigns every requested role in one call - a person can hold both ITAdministrator and
        // ITOperations at once, so the caller isn't restricted to granting a single role per
        // submission the way the account-provisioning step (find-or-create) only needs to happen
        // once regardless of how many roles are being granted.
        public async Task AssignRoleAsync(AssignHostRoleDto input)
        {
            var unsupportedRoleNames = input.RoleNames.Where(r => !AssignableRoleNames.Contains(r)).ToList();
            if (input.RoleNames.Length == 0 || unsupportedRoleNames.Count > 0)
            {
                throw new AbpValidationException($"Unsupported IT role(s): {string.Join(", ", unsupportedRoleNames)}");
            }

            var result = await _cssUsersApiService.FindUserAsync(input.Directory, input.Guid);
            if (result.Data == null || result.Data.Length == 0)
            {
                throw new AbpValidationException("User not found in directory");
            }

            var cssUser = result.Data[0];
            var oidcSub = (cssUser.Attributes?.IdirUserGuid?[0] ?? Guid.NewGuid().ToString()).ToSubjectWithoutIdp();
            var username = cssUser.Attributes?.IdirUsername?[0] ?? throw new AbpValidationException("Directory user has no username");
            var displayName = cssUser.Attributes?.DisplayName?[0] ?? username;

            var hostAccount = await _hostIdentityAccountManager.FindOrCreateHostAccountAsync(
                username, oidcSub, cssUser.FirstName, cssUser.LastName, cssUser.Email, displayName);

            foreach (var roleName in input.RoleNames)
            {
                await _hostIdentityAccountManager.AssignRoleClaimAsync(hostAccount, roleName);
            }
        }

        public async Task RevokeRoleAsync(Guid userId, string roleName)
        {
            var user = await _hostIdentityAccountManager.GetHostAccountAsync(userId);
            await _hostIdentityAccountManager.RevokeRoleClaimAsync(user, roleName);
        }
    }
}
