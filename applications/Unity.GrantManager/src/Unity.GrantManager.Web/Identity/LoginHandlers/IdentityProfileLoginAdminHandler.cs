using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Unity.GrantManager.Identity;
using Unity.TenantManagement;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Identity;

namespace Unity.GrantManager.Web.Identity.LoginHandlers
{
    internal class IdentityProfileLoginAdminHandler : IdentityProfileLoginBase
    {
        internal readonly ImmutableArray<string> _adminPermissions = ImmutableArray.Create(
            TenantManagementPermissions.Tenants.Default,
            TenantManagementPermissions.Tenants.Create,
            TenantManagementPermissions.Tenants.Update,
            TenantManagementPermissions.Tenants.Delete,
            TenantManagementPermissions.Tenants.ManageFeatures,
            TenantManagementPermissions.Tenants.ManageConnectionStrings,
            IdentityPermissions.Users.Create,
            IdentityPermissions.UserLookup.Default
        );

        internal async Task<UserTenantAccountDto> Handle(TokenValidatedContext validatedTokenContext,
           IList<UserTenantAccountDto> userTenantAccounts,
           string? idp)
        {
            UserTenantAccountDto userTenantAccount;
            if (!AdminHasUserAccount(userTenantAccounts))
            {
                // Create Host Account
                userTenantAccount = await CreateAdminAccountAsync(validatedTokenContext, idp);
            }
            else
            {
                userTenantAccount = userTenantAccounts.First(s => s.TenantId == null);
            }

            AssignAdminHostPermissions(validatedTokenContext.Principal!);
            AssignDefaultClaims(validatedTokenContext.Principal!, userTenantAccount.DisplayName ?? string.Empty, userTenantAccount.Id);
            return userTenantAccount;
        }

        private static bool AdminHasUserAccount(IList<UserTenantAccountDto>? userTenantAccounts)
        {
            if (userTenantAccounts == null || userTenantAccounts.Count == 0)
            {
                return false;
            }

            if (userTenantAccounts.Any(s => s.TenantId == null))
            {
                return true;
            }

            return false;
        }

        private void AssignAdminHostPermissions(ClaimsPrincipal claimsPrincipal)
        {
            claimsPrincipal.AddPermissions(_adminPermissions);
        }

        private async Task<UserTenantAccountDto> CreateAdminAccountAsync(TokenValidatedContext validatedTokenContext, string? idp)
        {
            var token = validatedTokenContext.SecurityToken;

            var userName = GetClaimValue(token, UnityClaimsTypes.PreferredUsername, idp);
            var displayName = GetClaimValue(token, UnityClaimsTypes.DisplayName, idp) ?? "DisplayName";
            var email = GetClaimValue(token, UnityClaimsTypes.Email, idp);
            var newUserId = Guid.NewGuid();

            var user = new IdentityUser(newUserId, userName, email ?? "blank@example.com", CurrentTenant.Id)
            {
                Name = GetClaimValue(token, UnityClaimsTypes.GivenName, idp),
                Surname = GetClaimValue(token, UnityClaimsTypes.FamilyName, idp)
            };

            user.SetEmailConfirmed(true);

            // Use identiy user manager to create the user
            user.SetProperty("OidcSub", validatedTokenContext.SecurityToken.Subject);
            user.SetProperty("DisplayName", displayName);

            var result = await IdentityUserManager.CreateAsync(user) ?? throw new AbpException("Error creating Identity User");

            if (!result.Succeeded)
            {
                throw new AbpException(string.Join('\n', result.Errors));
            }

            return new UserTenantAccountDto()
            {
                Id = user.Id,
                OidcSub = validatedTokenContext.SecurityToken.Subject,
                DisplayName = displayName,
                Username = user.UserName,
            };
        }
    }
}
