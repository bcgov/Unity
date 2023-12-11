using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using System;
using Volo.Abp.DependencyInjection;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Security.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using OpenIddict.Abstractions;
using System.Security.Claims;
using IdentityUser = Volo.Abp.Identity.IdentityUser;
using Volo.Abp.PermissionManagement;
using Unity.GrantManager.Permissions;
using Volo.Abp.SecurityLog;
using Volo.Abp.Data;
using Volo.Abp.TenantManagement;
using Unity.GrantManager.Identity;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Web.Identity
{
    internal class IdentityProfileLoginUpdater : ITransientDependency
    {
        private readonly IdentityUserManager _userManager;
        private readonly IdentityRoleManager _identityRoleManager;
        private readonly PermissionManager _permissionManager;
        private readonly ISecurityLogManager _securityLogManager;
        private readonly IIdentityUserRepository _identityUserRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IPersonRepository _personRepository;
        private readonly ICurrentTenant _currentTenant;

        public IdentityProfileLoginUpdater(IdentityUserManager userManager,
             IdentityRoleManager identityRoleManager,
             PermissionManager permissionManager,
             ISecurityLogManager securityLogManager,
             IIdentityUserRepository identityUserRepository,
             ITenantRepository tenantRepository,
             IPersonRepository personRepository,
             ICurrentTenant currentTenant)
        {
            _userManager = userManager;
            _identityRoleManager = identityRoleManager;
            _permissionManager = permissionManager;
            _securityLogManager = securityLogManager;
            _identityUserRepository = identityUserRepository;
            _tenantRepository = tenantRepository;
            _personRepository = personRepository;
            _currentTenant = currentTenant;
        }

        internal async Task CreateOrUpdateAsync(TokenValidatedContext validatedTokenContext)
        {
            // Default to the one default tenant for now - this will change once the multi part
            // of the tenancy is finished and users are created through user management
            var tenant = await _tenantRepository.FindByNameAsync(GrantManagerConsts.DefaultTenantName);

            using (_currentTenant.Change(tenant.Id))
            {
                var user = await ResolveUserAsync(validatedTokenContext);

                if (user == null)
                {
                    user = await CreateCurrentUserAsync(validatedTokenContext);
                }
                else
                {
                    await UpdateCurrentUserAsync(user, validatedTokenContext);
                }

                await SyncUserToTenantDatabase(user);
                await UpdatePrincipal(validatedTokenContext.Principal!, user, validatedTokenContext.SecurityToken);

                SetTokens(validatedTokenContext);

                // Create security log
                await _securityLogManager.SaveAsync(securityLog =>
                {
                    securityLog.Identity = validatedTokenContext.SecurityToken.Subject;
                    securityLog.Action = "Login";
                    securityLog.UserId = user.Id;
                    securityLog.UserName = validatedTokenContext.Principal!.GetClaim(UnityClaimsTypes.IDirUsername);
                });
            }
        }

        private async Task SyncUserToTenantDatabase(IdentityUser user)
        {
            var oidcSub = user.GetProperty("OidcSub")?.ToString();
            var displayName = user.GetProperty("DisplayName")?.ToString();

            // Create tenant level user
            if (oidcSub != null)
            {
                var existingUser = await _personRepository.FindByOidcSub(oidcSub);
                if (existingUser == null)
                {
                    var person = await _personRepository.InsertAsync(new Person()
                    {
                        Id = user.Id,
                        OidcSub = oidcSub,
                        OidcDisplayName = displayName ?? string.Empty,
                        FullName = $"{user.Name} {user.Surname}",
                        Badge = Utils.CreateUserBadge(user)
                    });
                    await _personRepository.UpdateAsync(person, true);
                }
            }
        }

        private async Task<IdentityUser?> ResolveUserAsync(TokenValidatedContext validatedTokenContext)
        {
            // support legacy user created, identity needs unique email address and could have been created by email first
            var email = GetClaimValue(validatedTokenContext.SecurityToken, AbpClaimTypes.Email);
            IdentityUser? user = null;

            if (email != null)
            {
                user = await _userManager.FindByEmailAsync(email);
            }

            if (user != null)
            {
                return user;
            }

            var userName = ResolveUsername(validatedTokenContext);
            user = await _userManager.FindByNameAsync(userName);

            return user;
        }

        private static string ResolveUsername(TokenValidatedContext validatedTokenContext)
        {
            var isDir = validatedTokenContext.SecurityToken.Subject.Contains("@idir");
            if (!isDir)
            {
                throw new NotImplementedException();
            }

            var usernameClaim = validatedTokenContext.SecurityToken.Claims.FirstOrDefault(s => s.Type == UnityClaimsTypes.IDirUsername);
            return usernameClaim == null ? throw new NotImplementedException() : usernameClaim.Value;
        }

        private static void SetTokens(TokenValidatedContext validatedTokenContext)
        {
            // Store access token in cookie, the refresh token still needs to be stored in db and refresh step created
            if (validatedTokenContext != null
                && validatedTokenContext.TokenEndpointResponse != null)
            {
                validatedTokenContext.Principal!.AddClaim("AccessToken", validatedTokenContext.TokenEndpointResponse.AccessToken);
            }
        }

        private async Task UpdatePrincipal(ClaimsPrincipal principal, IdentityUser user, JwtSecurityToken securityToken)
        {
            var adminRole = "admin";
            if (principal.IsInRole(adminRole))
            {
                principal.AddClaim(UnityClaimsTypes.Role, adminRole);
                var userPermissions = await _permissionManager.GetAllForRoleAsync(adminRole);
                foreach (var permission in userPermissions)
                {
                    principal.AddClaim("Permission", permission.Name);
                }
            }
            else
            {
                if (user.Roles != null)
                {
                    foreach (var role in user.Roles)
                    {
                        var dbRole = await _identityRoleManager.GetByIdAsync(role.RoleId);
                        principal.AddClaim(UnityClaimsTypes.Role, dbRole.Name);
                    }
                }

                var userPermissions = (await _permissionManager.GetAllForUserAsync(user.Id)).Where(s => s.IsGranted);

                foreach (var permissionName in userPermissions.Select(s => s.Name))
                {
                    if (!principal.HasClaim("Permission", permissionName))
                    {
                        principal.AddClaim("Permission", permissionName);
                    }
                }
            }

            principal.AddClaim(AbpClaimTypes.TenantId, _currentTenant?.Id?.ToString() ?? string.Empty);
            principal.AddClaim("DisplayName", GetClaimValue(securityToken, UnityClaimsTypes.DisplayName) ?? user.UserName);
            principal.AddClaim("UserId", user.Id.ToString());
            principal.AddClaim("Permission", GrantManagerPermissions.Default);
            principal.AddClaim("Permission", IdentityPermissions.UserLookup.Default);
        }

        protected virtual async Task<IdentityUser> CreateCurrentUserAsync(TokenValidatedContext validatedTokenContext)
        {
            var token = validatedTokenContext.SecurityToken;

            var userName = GetClaimValue(token, UnityClaimsTypes.IDirUsername);
            var displayName = GetClaimValue(token, UnityClaimsTypes.DisplayName) ?? "DisplayName";
            var email = GetClaimValue(token, AbpClaimTypes.Email);
            var newUserId = Guid.NewGuid();

            var user = new IdentityUser(newUserId, userName, email ?? "blank@example.com", _currentTenant.Id)
            {
                Name = GetClaimValue(token, UnityClaimsTypes.GivenName),
                Surname = GetClaimValue(token, UnityClaimsTypes.FamilyName)
            };

            var isEmailVerified = GetClaimValue(token, UnityClaimsTypes.EmailVerified) == "true";
            user.SetEmailConfirmed(isEmailVerified);

            if (!GetClaimValue(token, AbpClaimTypes.PhoneNumber).IsNullOrEmpty())
            {
                user.SetPhoneNumber(GetClaimValue(token, AbpClaimTypes.PhoneNumber), false);
            }

            // Use identiy user manager to create the user
            var result = await _userManager.CreateAsync(user) ?? throw new AbpException("Error creating Identity User");
            if (!result.Succeeded)
            {
                throw new AbpException(string.Join('\n', result.Errors));
            }

            var newUser = await _userManager.GetByIdAsync(user.Id);

            return await UpdateAdditionalUserPropertiesAsync(newUser, validatedTokenContext.SecurityToken.Subject, displayName);
        }

        private async Task<IdentityUser> UpdateAdditionalUserPropertiesAsync(IdentityUser user,
            string oidcSub,
            string displayName)
        {
            if (user != null)
            {
                user.SetProperty("OidcSub", oidcSub);
                user.SetProperty("DisplayName", displayName);
                await _identityUserRepository.UpdateAsync(user, true);
            }

            return user!;
        }

        protected virtual async Task UpdateCurrentUserAsync(IdentityUser user, TokenValidatedContext validatedTokenContext)
        {
            var token = validatedTokenContext.SecurityToken;

            if (user.Email != GetClaimValue(token, AbpClaimTypes.Email))
            {
                await _userManager.SetEmailAsync(user, GetClaimValue(token, AbpClaimTypes.Email) ?? "blank@example.com");
            }

            if (user.PhoneNumber != GetClaimValue(token, AbpClaimTypes.PhoneNumber))
            {
                await _userManager.SetPhoneNumberAsync(user, GetClaimValue(token, AbpClaimTypes.PhoneNumber));
            }

            await UpdateAdditionalUserPropertiesAsync(user, validatedTokenContext.SecurityToken.Subject, GetClaimValue(token, UnityClaimsTypes.DisplayName) ?? "DisplayName");
        }

        private static string? GetClaimValue(JwtSecurityToken token, string type)
        {
            return token.Claims.FirstOrDefault(s => s.Type == type)?.Value;
        }
    }
}
