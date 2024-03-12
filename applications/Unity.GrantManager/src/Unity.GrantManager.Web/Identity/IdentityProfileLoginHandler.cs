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
using Volo.Abp.DependencyInjection;
using Volo.Abp.SecurityLog;

namespace Unity.GrantManager.Web.Identity
{
    internal class IdentityProfileLoginHandler : ITransientDependency
    {
        private readonly IUserTenantAppService _userTenantsAppService;
        private readonly ISecurityLogManager _securityLogManager;

        public IdentityProfileLoginHandler(IUserTenantAppService userTenantsAppService,
            ISecurityLogManager securityLogManager)
        {
            _userTenantsAppService = userTenantsAppService;
            _securityLogManager = securityLogManager;
        }

        internal async Task HandleAsync(TokenValidatedContext validatedTokenContext)
        {
            if (validatedTokenContext.Principal != null)
            {
                var idpSplitter = validatedTokenContext.SecurityToken.Subject.IndexOf("@");
                var userIdentifier = validatedTokenContext.SecurityToken.Subject[..(idpSplitter == -1 ? validatedTokenContext.SecurityToken.Subject.Length : idpSplitter)].ToUpper();
                var userTenantAccounts = await _userTenantsAppService.GetUserTenantsAsync(userIdentifier);
                var idp = validatedTokenContext.SecurityToken.Claims.FirstOrDefault(s => s.Type == UnityClaimsTypes.IdpProvider)?.Value;

                UserTenantAccountDto signedInTenantAccount;

                if (validatedTokenContext.Principal.IsInRole(IdentityConsts.ITAdmin))
                {
                    var adminLoginHandler = validatedTokenContext.HttpContext.RequestServices.GetService<IdentityProfileLoginAdminHandler>();                    
                    signedInTenantAccount = await adminLoginHandler!.Handle(validatedTokenContext, userTenantAccounts, idp);
                }
                else
                {                    
                    var userLoginHandler = validatedTokenContext.HttpContext.RequestServices.GetService<IdentityProfileLoginUserHandler>();
                    signedInTenantAccount = await userLoginHandler!.Handle(validatedTokenContext, userTenantAccounts, idp);
                }

                AddTenantClaims(validatedTokenContext.Principal!, userTenantAccounts);

                // Create security log
                await _securityLogManager.SaveAsync(securityLog =>
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
            foreach (var tenantAcc in userTenantAccounts)
            {
                if (tenantAcc != null && tenantAcc.TenantId != null)
                {
                    claimsPrincipal.AddClaim(UnityClaimsTypes.Tenant, tenantAcc.TenantId.ToString() ?? Guid.Empty.ToString());
                }
            }
        }               
    }
}
