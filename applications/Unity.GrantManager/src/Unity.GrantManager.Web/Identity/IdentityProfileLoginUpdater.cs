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

namespace Unity.GrantManager.Web.Identity
{
    internal class IdentityProfileLoginUpdater : ITransientDependency
    {
        private readonly IdentityUserManager _userManager;
        private readonly IdentityRoleManager _identityRoleManager;
        private readonly PermissionManager _permissionManager;

        public IdentityProfileLoginUpdater(IdentityUserManager userManager,
             IdentityRoleManager identityRoleManager,
             PermissionManager permissionManager)
        {
            _userManager = userManager;
            _identityRoleManager = identityRoleManager;
            _permissionManager = permissionManager;
        }

        internal async Task UpdateAsync(TokenValidatedContext context)
        {
            await CreateOrUpdateAsync(context);
        }

        protected async Task CreateOrUpdateAsync(TokenValidatedContext validatedTokenContext)
        {
            var subject = validatedTokenContext.SecurityToken.Subject.Replace("@idir", "");
            var user = await _userManager.FindByIdAsync(subject);

            if (user == null)
            {
                await CreateCurrentUserAsync(validatedTokenContext);
                user = await _userManager.FindByIdAsync(subject);
            }
            else
            {
                await UpdateCurrentUserAsync(user, validatedTokenContext);
            }

            await UpdatePrincipal(validatedTokenContext.Principal!, user!);
        }

        private async Task UpdatePrincipal(ClaimsPrincipal principal, IdentityUser user)
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
                foreach (var role in user.Roles)
                {
                    var dbRole = await _identityRoleManager.GetByIdAsync(role.RoleId);
                    principal.AddClaim(UnityClaimsTypes.Role, dbRole.Name);

                    var userPermissions = (await _permissionManager.GetAllForUserAsync(user.Id)).Where(s => s.IsGranted);

                    foreach (var permission in userPermissions)
                        principal.AddClaim("Permission", permission.Name);
                }

            // This gets added as a Claim Provider through ABP - give this to all users for now
            principal.AddClaim("Permission", GrantManagerPermissions.Default);
            principal.AddClaim("Permission", IdentityPermissions.UserLookup.Default);
        }

        protected virtual async Task CreateCurrentUserAsync(TokenValidatedContext validatedTokenContext)
        {
            var token = validatedTokenContext.SecurityToken;
            var claims = token.Claims;

            var userNameClaim = claims.FirstOrDefault(x => x.Type == UnityClaimsTypes.Username);
            var user = new IdentityUser(
                    Guid.Parse(validatedTokenContext.SecurityToken.Subject.Replace("@idir", "")),
                    userNameClaim!.Value,
                    GetClaimValue(token, AbpClaimTypes.Email) ?? "blank@example.com")
            {
                Name = claims.FirstOrDefault(x => x.Type == UnityClaimsTypes.GivenName)?.Value,
                Surname = claims.FirstOrDefault(x => x.Type == UnityClaimsTypes.FamilyName)?.Value
            };

            var isEmailVerified = claims.FirstOrDefault(x => x.Type == "email_verified")?.Value == "true";
            user.SetEmailConfirmed(isEmailVerified);

            if (!GetClaimValue(token, AbpClaimTypes.PhoneNumber).IsNullOrEmpty())
            {
                user.SetPhoneNumber(GetClaimValue(token, AbpClaimTypes.PhoneNumber), false);
            }

            var result = await _userManager.CreateAsync(user);

            if (!result.Succeeded)
            {
                throw new AbpException(string.Join('\n', result.Errors));
            }
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
        }

        private static string? GetClaimValue(JwtSecurityToken token, string type)
        {
            return token.Claims.FirstOrDefault(s => s.Type == type)?.Value;
        }
    }
}
