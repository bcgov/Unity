using Microsoft.Extensions.DependencyInjection;
using System;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;

namespace Unity.GrantManager.Web.Identity
{
    public class AuthorizationConfiguration
    {
        internal static void Configure(ServiceConfigurationContext context)
        {                        
            context.Services.AddAuthorization(options =>
                  options.AddPolicy(IdentityPermissions.UserLookup.Default,
                  policy => policy.RequireClaim("Permission", IdentityPermissions.UserLookup.Default)));

            context.Services.AddAuthorization(options =>
                  options.AddPolicy(IdentityPermissions.Users.Default,
                  policy => policy.RequireClaim("Permission", IdentityPermissions.Users.Default)));

            // A policy that requires assertion
            context.Services.AddAuthorization(options => 
                  options.AddPolicy("SomeRandomAuthorization", 
                  policy => policy.RequireAssertion(context => 
                        context.User.HasClaim(c =>
                        (c.Type == "BadgeId" || c.Type == "TemporaryBadgeId")
                        && c.Issuer == "https://microsoftsecurity"))));
        }
    }
}
