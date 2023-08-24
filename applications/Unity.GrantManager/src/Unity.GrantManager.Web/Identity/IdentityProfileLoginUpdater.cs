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
using Microsoft.AspNetCore.Http;

namespace Unity.GrantManager.Web.Identity
{
    internal class IdentityProfileLoginUpdater : ITransientDependency
    {
        // The sample code for this hosts the Api in a host project with Bearer tokens
        // - this is a stripped down version to create the user on token validation while in monolith mode and relying on the proxy generation
        // private readonly IHttpClientFactory _httpClientFactory;                
        // private readonly IRemoteServiceConfigurationProvider _remoteServiceConfigurationProvider;
        // TODO: look at this during user sync / roles / permissions to make sure this works as wanted

        private readonly IdentityUserManager _userManager;

        public IdentityProfileLoginUpdater(IdentityUserManager userManager)
        {
            _userManager = userManager;
        }

        internal async Task UpdateAsync(TokenValidatedContext context)
        {
            await CreateOrUpdateAsync(context);
        }

        protected async Task CreateOrUpdateAsync(TokenValidatedContext validatedTokenContext)
        {
            var user = await _userManager.FindByIdAsync(validatedTokenContext.SecurityToken.Subject.Replace("@idir", ""));

            if (user == null)
            {
                await CreateCurrentUserAsync(validatedTokenContext);
            }
            else
            {
                await UpdateCurrentUserAsync(user, validatedTokenContext);
            }

            // This is needed for lookup - for now every user can get it until we lock this down further
            if (!validatedTokenContext.Principal!.HasClaim(UnityClaimsTypes.Role, IdentityPermissions.Users.Default))
            {
                validatedTokenContext.Principal!.AddClaim("Permission", IdentityPermissions.UserLookup.Default);
            }
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
