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
using Unity.GrantManager.Web.Exceptions;
using Volo.Abp.Security.Claims;

namespace Unity.GrantManager.Web.Identity.LoginHandlers
{
    internal class IdentityProfileLoginUserHandler : IdentityProfileLoginBase
    {
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

            // DB-managed roles (approver, assessor, program_manager, etc.) are no longer stamped
            // onto the principal here - ABP's dynamic claims refresh recomputes AbpClaimTypes.Role
            // from the DB on every request, so the cookie doesn't need to carry them. The
            // UnityClaimsTypes.Role ("client_roles") claim now only ever holds what Keycloak sends
            // natively (ITAdministrator/ITOperations), which ABP can't recompute.
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
    }
}
