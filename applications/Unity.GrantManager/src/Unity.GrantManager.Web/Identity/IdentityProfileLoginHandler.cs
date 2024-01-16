using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Permissions;
using Unity.GrantManager.Web.Exceptions;
using Unity.TenantManagement;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Security.Claims;
using Volo.Abp.SecurityLog;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace Unity.GrantManager.Web.Identity
{
    internal class IdentityProfileLoginHandler : ITransientDependency
    {
        private readonly IUserTenantAppService _userTenantsAppService;
        private readonly ICurrentTenant _currentTenant;
        private readonly ISecurityLogManager _securityLogManager;
        private readonly IIdentityUserRepository _identityUserRepository;

        private readonly IdentityUserManager _userManager;
        private readonly IdentityRoleManager _identityRoleManager;
        private readonly PermissionManager _permissionManager;

        public IdentityProfileLoginHandler(IUserTenantAppService userTenantsAppService,
            ICurrentTenant currentTenant,
            ISecurityLogManager securityLogManager,
            IIdentityUserRepository identityUserRepository,
            IdentityUserManager userManager,
            IdentityRoleManager identityRoleManager,
            PermissionManager permissionManager)
        {
            _userTenantsAppService = userTenantsAppService;
            _currentTenant = currentTenant;
            _securityLogManager = securityLogManager;
            _identityUserRepository = identityUserRepository;

            _userManager = userManager;
            _identityRoleManager = identityRoleManager;
            _permissionManager = permissionManager;
        }

        internal async Task HandleAsync(TokenValidatedContext validatedTokenContext)
        {
            if (validatedTokenContext.Principal != null)
            {
                var userTenantAccounts = await _userTenantsAppService.GetUserTenantsAsync(validatedTokenContext.SecurityToken.Subject);
                UserTenantAccountDto signedInTenantAccount;

                if (validatedTokenContext.Principal.IsInRole(IdentityConsts.ITAdmin))
                {
                    signedInTenantAccount = await HandleAdminLoginAsync(validatedTokenContext, userTenantAccounts);
                }
                else
                {
                    signedInTenantAccount = await HandleUserLoginAsync(validatedTokenContext, userTenantAccounts);
                }

                AddTenantClaims(validatedTokenContext.Principal!, userTenantAccounts);

                // Create security log
                await _securityLogManager.SaveAsync(securityLog =>
                {
                    securityLog.Identity = validatedTokenContext.SecurityToken.Subject;
                    securityLog.Action = "Login";
                    securityLog.UserId = signedInTenantAccount.Id;
                    securityLog.UserName = validatedTokenContext.Principal!.GetClaim(UnityClaimsTypes.IDirUsername);
                    securityLog.TenantId = signedInTenantAccount.TenantId;
                    securityLog.TenantName = signedInTenantAccount.TenantName;
                });
            }
        }

        private async Task<UserTenantAccountDto> HandleUserLoginAsync(TokenValidatedContext validatedTokenContext,
            IList<UserTenantAccountDto>? userTenantAccounts)
        {
            if (userTenantAccounts == null || userTenantAccounts.Count == 0)
            {
                // Needs to be called to sign out
                await validatedTokenContext.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await validatedTokenContext.HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
                throw new NoGrantProgramsLinkedException("User is not linked to any grant programs");
            }

            UserTenantAccountDto? userTenantAccount = null;
            var setTenant = validatedTokenContext.Request.Cookies["set_tenant"];
            if (setTenant != null)
            {
                userTenantAccount = userTenantAccounts.FirstOrDefault(s => s.TenantId != null && s.TenantId.ToString() == setTenant);
                validatedTokenContext.Response.Cookies.Append("set_tenant", setTenant, new Microsoft.AspNetCore.Http.CookieOptions() { Expires = DateTime.UtcNow.AddDays(-1) });
            }

            if (userTenantAccount == null)
            {
                userTenantAccount = userTenantAccounts[0];
            }

            var principal = validatedTokenContext.Principal!;

            using (_currentTenant.Change(userTenantAccount.TenantId))
            {
                var userRoles = await _identityUserRepository.GetRolesAsync(userTenantAccount.Id);

                if (userRoles != null)
                {
                    foreach (var role in userRoles)
                    {
                        var dbRole = await _identityRoleManager.GetByIdAsync(role.Id);
                        principal.AddClaim(UnityClaimsTypes.Role, dbRole.Name);
                    }
                }

                var userPermissions = (await _permissionManager.GetAllForUserAsync(userTenantAccount.Id)).Where(s => s.IsGranted);

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

        private async Task<UserTenantAccountDto> HandleAdminLoginAsync(TokenValidatedContext validatedTokenContext,
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

        private static void AddTenantClaims(ClaimsPrincipal claimsPrincipal, IList<UserTenantAccountDto> userTenantAccounts)
        {
            foreach (var tenantAcc in userTenantAccounts)
            {
                if (tenantAcc != null && tenantAcc.TenantId != null)
                {
                    claimsPrincipal.AddClaim("tenant", tenantAcc.TenantId.ToString() ?? Guid.Empty.ToString());
                }
            }
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

            var user = new IdentityUser(newUserId, userName, email ?? "blank@example.com", _currentTenant.Id)
            {
                Name = GetClaimValue(token, UnityClaimsTypes.GivenName),
                Surname = GetClaimValue(token, UnityClaimsTypes.FamilyName)
            };

            user.SetEmailConfirmed(true);

            // Use identiy user manager to create the user
            user.SetProperty("OidcSub", validatedTokenContext.SecurityToken.Subject);
            user.SetProperty("DisplayName", displayName);

            var result = await _userManager.CreateAsync(user) ?? throw new AbpException("Error creating Identity User");

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

        private static void AssignDefaultClaims(ClaimsPrincipal claimsPrinicipal,
            string displayName,
            Guid userId)
        {
            claimsPrinicipal.AddClaim("DisplayName", displayName);
            claimsPrinicipal.AddClaim("UserId", userId.ToString());
        }

        private static void AssignDefaultPermissions(ClaimsPrincipal claimsPrincipal)
        {
            claimsPrincipal.AddClaim("Permission", GrantManagerPermissions.Default);
            claimsPrincipal.AddClaim("Permission", IdentityPermissions.UserLookup.Default);
        }

        private static string? GetClaimValue(JwtSecurityToken token, string type)
        {
            return token.Claims.FirstOrDefault(s => s.Type == type)?.Value;
        }
    }
}
