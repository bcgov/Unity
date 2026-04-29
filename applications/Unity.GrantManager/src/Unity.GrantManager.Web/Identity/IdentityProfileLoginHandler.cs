using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Web.Identity.LoginHandlers;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.SecurityLog;

namespace Unity.GrantManager.Web.Identity
{
    internal class IdentityProfileLoginHandler(
        IUserTenantAppService userTenantsAppService,
        ISecurityLogManager securityLogManager) : ITransientDependency
    {
        internal async Task HandleAsync(TokenValidatedContext validatedTokenContext)
        {
            var principal = validatedTokenContext.Principal;
            var subject = validatedTokenContext.SecurityToken?.Subject;

            if (principal == null || string.IsNullOrWhiteSpace(subject))
            {
                return;
            }

            // Extract user identifier safely
            var userIdentifier = ExtractUserIdentifier(subject);

            // Fetch tenant accounts once
            var userTenantAccounts = await userTenantsAppService.GetUserTenantsAsync(userIdentifier) 
                                     ?? new List<UserTenantAccountDto>();

            var idp = validatedTokenContext.SecurityToken?.Claims
                .FirstOrDefault(c => c.Type == UnityClaimsTypes.IdpProvider)?.Value;

            UserTenantAccountDto? signedInTenantAccount;

            if (principal.IsInRole(IdentityConsts.ITAdminRoleName))
            {
                var adminLoginHandler = validatedTokenContext.HttpContext.RequestServices
                    .GetService<IdentityProfileLoginAdminHandler>();

                if (adminLoginHandler == null)
                {
                    throw new InvalidOperationException("IdentityProfileLoginAdminHandler is not registered.");
                }

                var adminAccount = await userTenantsAppService.GetUserAdminAccountAsync(userIdentifier);

                if (adminAccount != null && userTenantAccounts.All(x => x.TenantId != adminAccount.TenantId))
                {
                    userTenantAccounts.Add(adminAccount);
                }

                signedInTenantAccount = await adminLoginHandler.Handle(
                    validatedTokenContext,
                    userTenantAccounts,
                    idp);
            }
            else
            {
                var userLoginHandler = validatedTokenContext.HttpContext.RequestServices
                    .GetService<IdentityProfileLoginUserHandler>();

                if (userLoginHandler == null)
                {
                    throw new InvalidOperationException("IdentityProfileLoginUserHandler is not registered.");
                }

                signedInTenantAccount = await userLoginHandler.Handle(
                    validatedTokenContext,
                    userTenantAccounts,
                    idp);
            }

            if (signedInTenantAccount == null)
            {
                throw new InvalidOperationException("No tenant account could be resolved during login.");
            }

            AddTenantClaims(principal, userTenantAccounts);

            // Create security log safely
            await securityLogManager.SaveAsync(securityLog =>
            {
                securityLog.Identity = subject;
                securityLog.Action = "Login";
                securityLog.UserId = signedInTenantAccount.Id;
                securityLog.UserName = principal.GetClaim(
                    UnityClaimsResolver.ResolveFor(UnityClaimsTypes.PreferredUsername, idp)
                ) ?? "UNKNOWN";
                securityLog.TenantId = signedInTenantAccount.TenantId;
                securityLog.TenantName = signedInTenantAccount.TenantName;
            });
        }

        private static string ExtractUserIdentifier(string subject)
        {
            var atIndex = subject.IndexOf('@');
            var identifier = atIndex >= 0 ? subject[..atIndex] : subject;
            return identifier.ToUpperInvariant();
        }

        private static void AddTenantClaims(
            ClaimsPrincipal claimsPrincipal,
            IList<UserTenantAccountDto> userTenantAccounts)
        {
            // Hard safety check: if too many tenants, skip adding claims and log warning (or switch to server-side)
            if (userTenantAccounts.Count > 20)
            {
                // TODO: log warning or switch to server-side tenant resolution
                return;
            }

            // Only store active tenant (if available)
            var activeTenant = userTenantAccounts.FirstOrDefault(x => x?.TenantId != null);
            if (activeTenant != null)
            {
                claimsPrincipal.AddClaim(UnityClaimsTypes.Tenant, activeTenant.TenantId!.Value.ToString());
                return;
            }

            // If you must store multiple, compress into a single claim (not ideal)
            var distinctTenants = userTenantAccounts
                .Where(x => x?.TenantId != null)
                .Select(x => x.TenantId!.Value.ToString())
                .Distinct()
                .ToList();
            if (distinctTenants.Count > 0)
            {
                claimsPrincipal.AddClaim("tenant_ids", string.Join(",", distinctTenants));
            }
        }
    }
}