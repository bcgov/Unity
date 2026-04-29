using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
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
using Microsoft.Extensions.Logging;

namespace Unity.GrantManager.Web.Identity
{
    internal class IdentityProfileLoginHandler : ITransientDependency
    {
        private readonly ILogger<IdentityProfileLoginHandler> _logger;
        private readonly IUserTenantAppService _userTenantsAppService;
        private readonly ISecurityLogManager _securityLogManager;

        internal IdentityProfileLoginHandler(
            IUserTenantAppService userTenantsAppService,
            ISecurityLogManager securityLogManager,
            ILogger<IdentityProfileLoginHandler> logger)
        {
            _userTenantsAppService = userTenantsAppService ?? throw new ArgumentNullException(nameof(userTenantsAppService));
            _securityLogManager = securityLogManager ?? throw new ArgumentNullException(nameof(securityLogManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        internal async Task HandleAsync(TokenValidatedContext validatedTokenContext)
        {
            var principal = validatedTokenContext.Principal;

            var subject = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (principal == null || string.IsNullOrWhiteSpace(subject))
            {
                return;
            }

            var userIdentifier = ExtractUserIdentifier(subject);

            var userTenantAccounts =
                await _userTenantsAppService.GetUserTenantsAsync(userIdentifier)
                ?? new List<UserTenantAccountDto>();

            var idp = validatedTokenContext.SecurityToken?.Claims
                .FirstOrDefault(c => c.Type == UnityClaimsTypes.IdpProvider)?.Value;

            UserTenantAccountDto? signedInTenantAccount;

            if (principal.IsInRole(IdentityConsts.ITAdminRoleName))
            {
                var adminLoginHandler = validatedTokenContext.HttpContext.RequestServices
                    .GetRequiredService<IdentityProfileLoginAdminHandler>();

                var adminAccount =
                    await _userTenantsAppService.GetUserAdminAccountAsync(userIdentifier);

                if (adminAccount != null &&
                    userTenantAccounts.All(x => x.TenantId != adminAccount.TenantId))
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
                    .GetRequiredService<IdentityProfileLoginUserHandler>();

                signedInTenantAccount = await userLoginHandler.Handle(
                    validatedTokenContext,
                    userTenantAccounts,
                    idp);
            }

            if (signedInTenantAccount == null)
            {
                throw new InvalidOperationException(
                    "No tenant account could be resolved during login.");
            }

            AddTenantClaims(principal, userTenantAccounts, _logger);

            await _securityLogManager.SaveAsync(securityLog =>
            {
                securityLog.Identity = subject;
                securityLog.Action = "Login";
                securityLog.UserId = signedInTenantAccount.Id;
                securityLog.UserName = principal.FindFirst(
                    UnityClaimsResolver.ResolveFor(UnityClaimsTypes.PreferredUsername, idp)
                )?.Value ?? "UNKNOWN";

                securityLog.TenantId = signedInTenantAccount.TenantId;
                securityLog.TenantName = signedInTenantAccount.TenantName;
            });
        }

        private static string ExtractUserIdentifier(string subject)
        {
            var atIndex = subject.IndexOf('@');
            return (atIndex >= 0 ? subject[..atIndex] : subject)
                .ToUpperInvariant();
        }

        private static void AddTenantClaims(
            ClaimsPrincipal claimsPrincipal,
            IList<UserTenantAccountDto> userTenantAccounts,
            ILogger logger)
        {
            if (userTenantAccounts.Count > 20)
            {
                logger.LogWarning(
                    "Too many tenants in claims: {Count}. Claims not added.",
                    userTenantAccounts.Count);

                return;
            }

            var identity = claimsPrincipal.Identity as ClaimsIdentity;
            if (identity == null)
            {
                logger.LogWarning(
                    "No ClaimsIdentity found. Cannot add tenant claims.");
                return;
            }

            var activeTenant =
                userTenantAccounts.FirstOrDefault(x => x?.TenantId != null);

            if (activeTenant != null)
            {
                identity.AddClaim(new Claim(
                    UnityClaimsTypes.Tenant,
                    activeTenant.TenantId!.Value.ToString()));

                return;
            }

            var distinctTenants = userTenantAccounts
                .Where(x => x?.TenantId != null)
                .Select(x => x.TenantId!.Value.ToString())
                .Distinct()
                .ToList();

            if (distinctTenants.Count > 0)
            {
                identity.AddClaim(new Claim(
                    "tenant_ids",
                    string.Join(",", distinctTenants)));
            }
        }
    }
}