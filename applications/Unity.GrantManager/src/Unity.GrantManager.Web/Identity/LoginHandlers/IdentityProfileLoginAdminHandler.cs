using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
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
        internal async Task<UserTenantAccountDto> Handle(TokenValidatedContext validatedTokenContext,
           IList<UserTenantAccountDto> userTenantAccounts)
        {
            UserTenantAccountDto userTenantAccount;
            if (!AdminHasUserAccount(userTenantAccounts))
            {
                // Create Host Account
                userTenantAccount = await CreateAdminAccountAsync(validatedTokenContext);
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

        private static void AssignAdminHostPermissions(ClaimsPrincipal claimsPrincipal)
        {
            claimsPrincipal.AddClaim("Permission", TenantManagementPermissions.Tenants.Default);
            claimsPrincipal.AddClaim("Permission", TenantManagementPermissions.Tenants.Create);
            claimsPrincipal.AddClaim("Permission", TenantManagementPermissions.Tenants.Update);
            claimsPrincipal.AddClaim("Permission", TenantManagementPermissions.Tenants.Delete);
            claimsPrincipal.AddClaim("Permission", TenantManagementPermissions.Tenants.ManageFeatures);
            claimsPrincipal.AddClaim("Permission", TenantManagementPermissions.Tenants.ManageConnectionStrings);
            claimsPrincipal.AddClaim("Permission", IdentityPermissions.Users.Create);
            claimsPrincipal.AddClaim("Permission", IdentityPermissions.UserLookup.Default);
        }

        private async Task<UserTenantAccountDto> CreateAdminAccountAsync(TokenValidatedContext validatedTokenContext)
        {
            var token = validatedTokenContext.SecurityToken;

            var userName = GetClaimValue(token, UnityClaimsTypes.IDirUsername);
            var displayName = GetClaimValue(token, UnityClaimsTypes.DisplayName) ?? "DisplayName";
            var email = GetClaimValue(token, UnityClaimsTypes.Email);
            var newUserId = Guid.NewGuid();

            var user = new IdentityUser(newUserId, userName, email ?? "blank@example.com", CurrentTenant.Id)
            {
                Name = GetClaimValue(token, UnityClaimsTypes.GivenName),
                Surname = GetClaimValue(token, UnityClaimsTypes.FamilyName)
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
