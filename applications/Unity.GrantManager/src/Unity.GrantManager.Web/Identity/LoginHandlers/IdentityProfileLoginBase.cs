using System.Security.Claims;
using System;
using System.Security.Principal;
using Unity.GrantManager.Identity;
using OpenIddict.Abstractions;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Volo.Abp.MultiTenancy;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.Security.Claims;
using Microsoft.Extensions.Configuration;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Web.Identity.LoginHandlers
{
    internal abstract class IdentityProfileLoginBase : ITransientDependency
    {
        public IAbpLazyServiceProvider LazyServiceProvider { get; set; } = default!;
        protected ICurrentTenant CurrentTenant => LazyServiceProvider.LazyGetRequiredService<ICurrentTenant>();
        protected IdentityUserManager IdentityUserManager => LazyServiceProvider.LazyGetRequiredService<IdentityUserManager>();
        protected IdentityRoleManager IdentityRoleManager => LazyServiceProvider.LazyGetRequiredService<IdentityRoleManager>();
        protected IIdentityUserRepository IdentityUserRepository => LazyServiceProvider.LazyGetRequiredService<IIdentityUserRepository>();
        protected IConfiguration Configuration => LazyServiceProvider.LazyGetRequiredService<IConfiguration>();
        protected IUserImportAppService UserImportAppService => LazyServiceProvider.LazyGetRequiredService<IUserImportAppService>();
        protected ITenantRepository TenantRepository => LazyServiceProvider.LazyGetRequiredService<ITenantRepository>();
        protected IUserTenantAppService UserTenantsAppService => LazyServiceProvider.LazyGetRequiredService<IUserTenantAppService>();

        protected static void AssignDefaultClaims(ClaimsPrincipal claimsPrinicipal, string displayName, Guid userId)
        {
            // AbpClaimTypes.UserId is the same claim type URI as ClaimTypes.NameIdentifier, which the
            // OIDC/JWT handler already populates from the token's "sub" claim before this runs. Without
            // clearing it first, the principal ends up with two UserId claims (Keycloak's sub, then ours),
            // and CurrentUser.FindUserId()'s FirstOrDefault picks the wrong (sub) one.
            var identity = claimsPrinicipal.Identity as ClaimsIdentity;
            identity?.RemoveAll(AbpClaimTypes.UserId);

            claimsPrinicipal.AddClaim("DisplayName", displayName);
            claimsPrinicipal.AddClaim(AbpClaimTypes.UserId, userId.ToString());
            claimsPrinicipal.AddClaim("Badge", Utils.CreateUserBadge(displayName));
        }

        protected static string? GetClaimValue(JwtSecurityToken token, string type, string? idp)
        {
            type = UnityClaimsResolver.ResolveFor(type, idp);
            return token.Claims.FirstOrDefault(s => s.Type == type)?.Value;
        }
    }
}
