using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Web.Identity.Authorization;
using Unity.GrantManager.Web.Identity.LoginHandlers;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SecurityLog;

namespace Unity.GrantManager.Web.Identity
{
    internal class IdentityProfileLoginHandler(IUserTenantAppService userTenantsAppService,
        ISecurityLogManager securityLogManager,
        ICurrentTenant currentTenant,
        IUserClaimsRoleChecker userClaimsRoleChecker) : ITransientDependency
    {
        internal async Task HandleAsync(TokenValidatedContext validatedTokenContext)
        {
            if (validatedTokenContext.Principal != null)
            {
                var idpSplitter = validatedTokenContext.SecurityToken.Subject.IndexOf('@');
                var userIdentifier = validatedTokenContext.SecurityToken.Subject[..(idpSplitter == -1 ? validatedTokenContext.SecurityToken.Subject.Length : idpSplitter)].ToUpper();
                var userTenantAccounts = await userTenantsAppService.GetUserTenantsAsync(userIdentifier);
                var idp = validatedTokenContext.SecurityToken.Claims.FirstOrDefault(s => s.Type == UnityClaimsTypes.IdpProvider)?.Value;

                UserTenantAccountDto signedInTenantAccount;

                // Looked up unconditionally (not just when the token already claims ITAdministrator)
                // so a role claim stamped directly in the UserClaims table - for environments where
                // Keycloak's client_roles token claim isn't reliably mapped - can also grant host
                // access. Stamping it onto the principal here, before the routing check below, makes
                // it behave exactly as if Keycloak had sent it: it drives the admin-vs-tenant routing
                // decision and, once in the resulting cookie, every downstream IsInRole/menu/policy
                // check that reads client_roles from the principal sees it too.
                var adminAccount = await userTenantsAppService.GetUserAdminAccountAsync(userIdentifier);
                if (adminAccount != null)
                {
                    await StampUserClaimsRoleFallbackAsync(validatedTokenContext.Principal, adminAccount.Id, null);
                }

                if (validatedTokenContext.Principal.IsInRole(IdentityConsts.ITAdminRoleName))
                {
                    var adminLoginHandler = validatedTokenContext.HttpContext.RequestServices.GetService<IdentityProfileLoginAdminHandler>();
                    if (adminAccount != null)
                    {
                        userTenantAccounts.Add(adminAccount);
                    }
                    signedInTenantAccount = await adminLoginHandler!.Handle(validatedTokenContext, userTenantAccounts, idp);
                }
                else
                {
                    var userLoginHandler = validatedTokenContext.HttpContext.RequestServices.GetService<IdentityProfileLoginUserHandler>();
                    signedInTenantAccount = await userLoginHandler!.Handle(validatedTokenContext, userTenantAccounts, idp);
                }

                // Covers ITOperations (or ITAdministrator) claimed via the UserClaims table on the
                // account actually signed into, when that's a tenant account rather than the host
                // account handled above.
                await StampUserClaimsRoleFallbackAsync(validatedTokenContext.Principal, signedInTenantAccount.Id, signedInTenantAccount.TenantId);

                AddTenantClaims(validatedTokenContext.Principal!, userTenantAccounts);
                RemoveRawJwtMetadataClaims(validatedTokenContext.Principal!);

                // Create security log
                await securityLogManager.SaveAsync(securityLog =>
                {
                    securityLog.Identity = validatedTokenContext.SecurityToken.Subject;
                    securityLog.Action = "Login";
                    securityLog.UserId = signedInTenantAccount.Id;
                    securityLog.UserName = validatedTokenContext.Principal!.GetClaim(UnityClaimsResolver.ResolveFor(UnityClaimsTypes.PreferredUsername, idp));
                    securityLog.TenantId = signedInTenantAccount.TenantId;
                    securityLog.TenantName = signedInTenantAccount.TenantName;
                });
            }
        }

        // Adds a client_roles claim for any ITAdministrator/ITOperations role that the given
        // account has as a UserClaims table entry but that isn't already present on the principal
        // (i.e. wasn't sent by Keycloak). Runs the DB lookup under the account's own tenant context
        // (null for the host account) so the multi-tenancy data filter on IdentityUserClaim matches
        // the row - see UserClaimsRoleChecker.
        private async Task StampUserClaimsRoleFallbackAsync(ClaimsPrincipal principal, Guid userId, Guid? tenantId)
        {
            var existingRoleValues = principal.Claims
                .Where(c => c.Type == UnityClaimsTypes.Role)
                .Select(c => c.Value)
                .ToHashSet();

            var missingRoles = new[] { IdentityConsts.ITAdminRoleName, IdentityConsts.ITOperationsRoleName }
                .Where(roleName => !existingRoleValues.Contains(roleName))
                .ToArray();

            if (missingRoles.Length == 0)
            {
                return;
            }

            using (currentTenant.Change(tenantId))
            {
                foreach (var roleName in missingRoles)
                {
                    if (await userClaimsRoleChecker.HasAnyRoleAsync(userId, [roleName]))
                    {
                        principal.AddClaim(UnityClaimsTypes.Role, roleName);
                    }
                }
            }
        }

        private static void AddTenantClaims(ClaimsPrincipal claimsPrincipal, IList<UserTenantAccountDto> userTenantAccounts)
        {
            if (userTenantAccounts.Count > 1)
            {
                claimsPrincipal.AddClaim(UnityClaimsTypes.HasMultipleTenants, "true");
            }
        }

        // Raw OIDC/JWT protocol claims that ASP.NET Core attaches to the principal when it parses
        // the ID token, unrelated to app-level identity/authorization and unused anywhere in this
        // codebase (verified via repo-wide search) - stripped before sign-in to shrink the auth cookie.
        private static readonly string[] RawJwtMetadataClaimTypes =
        [
            "jti", "sid", "session_state", "at_hash", "iss", "aud", "azp", "exp", "iat", "typ"
        ];

        private static void RemoveRawJwtMetadataClaims(ClaimsPrincipal claimsPrincipal)
        {
            var identity = claimsPrincipal.Identity as ClaimsIdentity;
            foreach (var claimType in RawJwtMetadataClaimTypes)
            {
                identity?.RemoveAll(claimType);
            }
        }
    }
}
