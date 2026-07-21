using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Web.Identity.LoginHandlers;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.SecurityLog;

namespace Unity.GrantManager.Web.Identity
{
    internal class IdentityProfileLoginHandler(IUserTenantAppService userTenantsAppService,
        ISecurityLogManager securityLogManager) : ITransientDependency
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

                if (validatedTokenContext.Principal.IsInRole(IdentityConsts.ITAdminRoleName))
                {
                    var adminLoginHandler = validatedTokenContext.HttpContext.RequestServices.GetService<IdentityProfileLoginAdminHandler>();
                    var adminAccount = await userTenantsAppService.GetUserAdminAccountAsync(userIdentifier);
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
