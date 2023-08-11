using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using System;
using Volo.Abp.DependencyInjection;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Security.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Unity.GrantManager.Web.Identity
{
    internal class IdentityProfileLoginUpdater : ITransientDependency
    {
        // The sample code for this hosts the Api in a host project with Bearer tokens
        // - this is a stripped down version to create the user on token validation while in monolith mode and relying on the proxy generation
        // private readonly IHttpClientFactory _httpClientFactory;                
        // private readonly IRemoteServiceConfigurationProvider _remoteServiceConfigurationProvider;

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
            var user = await _userManager.FindByIdAsync(validatedTokenContext.SecurityToken.Subject);

            if (user == null)
            {
                await CreateCurrentUserAsync(validatedTokenContext);
            }
            else
            {
                await UpdateCurrentUserAsync(user, validatedTokenContext);
            }
        }

        protected virtual async Task CreateCurrentUserAsync(TokenValidatedContext validatedTokenContext)
        {
            var token = validatedTokenContext.SecurityToken;
            var claims = token.Claims;

            var userNameClaim = claims.FirstOrDefault(x => x.Type == "preferred_username");

            var user = new IdentityUser(
                    Guid.Parse(validatedTokenContext.SecurityToken.Subject),
                    userNameClaim!.Value, //CurrentUser.UserName provides FullName instead of UserName
                    GetClaimValue(token, AbpClaimTypes.Email) ?? "blank@blank.blank");
            // abp want an email to create user locally
            //CurrentTenant.Id);

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
                await _userManager.SetEmailAsync(user, GetClaimValue(token, AbpClaimTypes.Email) ?? "blank@blank.blank");
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
