using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Permissions;
using Unity.GrantManager.Web.Exceptions;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Security.Claims;

namespace Unity.GrantManager.Web.Identity.LoginHandlers
{
    internal class IdentityProfileLoginUserHandler : IdentityProfileLoginBase
    {
        internal async Task<UserTenantAccountDto> Handle(TokenValidatedContext validatedTokenContext,
          IList<UserTenantAccountDto>? userTenantAccounts)
        {
            // filter out host account if coming in as tenant - add support for this later
            userTenantAccounts = userTenantAccounts?.Where(s => s.TenantId != null).ToList();
            if (userTenantAccounts == null || userTenantAccounts.Count == 0)
            {
                if (UseAutoRegisterUserWithDefault())
                {
                    userTenantAccounts = await AutoRegisterUserWithDefaultAsync();
                }
                else
                {
                    throw new NoGrantProgramsLinkedException("User is not linked to any grant programs");
                }
            }

            UserTenantAccountDto? userTenantAccount = null;
            var setTenant = validatedTokenContext.Request.Cookies["set_tenant"];
            if (setTenant != null && setTenant != Guid.Empty.ToString())
            {
                userTenantAccount = userTenantAccounts.FirstOrDefault(s => s.TenantId.ToString() == setTenant);
                validatedTokenContext.Response.Cookies.Append("set_tenant", setTenant, new Microsoft.AspNetCore.Http.CookieOptions()
                { Expires = DateTime.UtcNow.AddDays(-1), Secure = true, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None, HttpOnly = true });
            }

            userTenantAccount ??= userTenantAccounts[0];
            var principal = validatedTokenContext.Principal!;

            using (CurrentTenant.Change(userTenantAccount.TenantId))
            {
                var userRoles = await IdentityUserRepository.GetRolesAsync(userTenantAccount.Id);

                if (userRoles != null)
                {
                    foreach (var role in userRoles)
                    {
                        var dbRole = await IdentityRoleManager.GetByIdAsync(role.Id);
                        principal.AddClaim(UnityClaimsTypes.Role, dbRole.Name);
                    }
                }

                var userPermissions = (await PermissionManager.GetAllForUserAsync(userTenantAccount.Id)).Where(s => s.IsGranted);

                foreach (var permissionName in userPermissions.Select(s => s.Name))
                {
                    if (!principal.HasClaim("Permission", permissionName))
                    {
                        principal.AddClaim("Permission", permissionName);
                    }
                }
            }

            AssignDefaultPermissions(validatedTokenContext.Principal!);
            AssignDefaultClaims(validatedTokenContext.Principal!, userTenantAccount.DisplayName ?? string.Empty, userTenantAccount.Id);

            validatedTokenContext.Principal!.AddClaim(AbpClaimTypes.TenantId, userTenantAccount.TenantId?.ToString() ?? Guid.Empty.ToString());

            return userTenantAccount;
        }

        private Task<IList<UserTenantAccountDto>> AutoRegisterUserWithDefaultAsync()
        {
            throw new NotImplementedException();
        }

        private bool UseAutoRegisterUserWithDefault()
        {
            // we really only want this for local development
            return IsEnvironmentDevelopment() && IsAutoRegisterFlagSet();
        }

        private static bool IsEnvironmentDevelopment()
        {
            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == Environments.Development;
        }

        private bool IsAutoRegisterFlagSet()
        {
            return Configuration.GetValue<bool>("IdentityProfileLogin:AutoCreateUser");            
        }

        private static void AssignDefaultPermissions(ClaimsPrincipal claimsPrincipal)
        {
            claimsPrincipal.AddClaim("Permission", GrantManagerPermissions.Default);
            claimsPrincipal.AddClaim("Permission", IdentityPermissions.UserLookup.Default);
        }
    }
}
