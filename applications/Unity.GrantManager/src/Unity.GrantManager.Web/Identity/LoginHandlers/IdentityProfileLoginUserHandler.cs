using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        internal readonly ImmutableArray<string> _userPermissions = ImmutableArray
            .Create(GrantManagerPermissions.Default, IdentityPermissions.UserLookup.Default);

        internal async Task<UserTenantAccountDto> Handle(TokenValidatedContext validatedTokenContext,
          IList<UserTenantAccountDto>? userTenantAccounts,
          string? idp)
        {
            // filter out host account if coming in as tenant - add support for this later
            userTenantAccounts = userTenantAccounts?.Where(s => s.TenantId != null).ToList();
            if (userTenantAccounts == null || userTenantAccounts.Count == 0)
            {
                if (UseAutoRegisterUserWithDefault())
                {
                    var token = validatedTokenContext.SecurityToken;
                    var userSubject = GetClaimValue(validatedTokenContext.SecurityToken, UnityClaimsTypes.Subject, idp) ?? throw new AutoRegisterUserException("Error auto registering user");
                    var userIdentifier = userSubject.ToSubjectWithoutIdp();
                    userTenantAccounts = await AutoRegisterUserWithDefaultAsync(userIdentifier,
                        GetClaimValue(token, UnityClaimsTypes.PreferredUsername, idp) ?? UnityClaimsTypes.PreferredUsername,
                        GetClaimValue(token, UnityClaimsTypes.GivenName, idp) ?? UnityClaimsTypes.GivenName,
                        GetClaimValue(token, UnityClaimsTypes.FamilyName, idp) ?? UnityClaimsTypes.FamilyName,
                        GetClaimValue(token, UnityClaimsTypes.Email, idp) ?? UnityClaimsTypes.Email,
                        userIdentifier,
                        GetClaimValue(token, UnityClaimsTypes.DisplayName, idp) ?? UnityClaimsTypes.DisplayName);
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
                    if (!principal.HasClaim(UnityClaimsTypes.Permission, permissionName))
                    {
                        principal.AddClaim(UnityClaimsTypes.Permission, permissionName);
                    }
                }
            }

            AssignDefaultPermissions(validatedTokenContext.Principal!);
            AssignDefaultClaims(validatedTokenContext.Principal!, userTenantAccount.DisplayName ?? string.Empty, userTenantAccount.Id);

            validatedTokenContext.Principal!.AddClaim(AbpClaimTypes.TenantId, userTenantAccount.TenantId?.ToString() ?? Guid.Empty.ToString());

            return userTenantAccount;
        }

        private async Task<IList<UserTenantAccountDto>> AutoRegisterUserWithDefaultAsync(string userIdentifier,
            string username,
            string firstName,
            string lastName,
            string emailAddress,
            string oidcSub,
            string displayName)
        {
            var tenant = await TenantRepository.FindByNameAsync(GrantManagerConsts.NormalizedDefaultTenantName);

            using (CurrentTenant.Change(tenant.Id))
            {
                await UserImportAppService.AutoImportUserInternalAsync(new ImportUserDto()
                {
                    Directory = "IDIR",
                    Guid = userIdentifier,
                    Roles = [UnityRoles.ProgramManager]
                }, username, firstName, lastName, emailAddress, oidcSub, displayName);

                // Re-read tenant accounts and return
                return (await UserTenantsAppService
                    .GetUserTenantsAsync(userIdentifier))
                    .Where(s => s.TenantId != null).ToList();
            }
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

        private void AssignDefaultPermissions(ClaimsPrincipal claimsPrincipal)
        {
            claimsPrincipal.AddPermissions(_userPermissions);
        }
    }
}
