using System.Security.Claims;
using System;
using Unity.GrantManager.Identity;
using OpenIddict.Abstractions;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Volo.Abp.MultiTenancy;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
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
        protected PermissionManager PermissionManager => LazyServiceProvider.LazyGetRequiredService<PermissionManager>();
        protected IIdentityUserRepository IdentityUserRepository => LazyServiceProvider.LazyGetRequiredService<IIdentityUserRepository>();
        protected IConfiguration Configuration => LazyServiceProvider.LazyGetRequiredService<IConfiguration>();
        protected IUserImportAppService UserImportAppService => LazyServiceProvider.LazyGetRequiredService<IUserImportAppService>();
        protected ITenantRepository TenantRepository => LazyServiceProvider.LazyGetRequiredService<ITenantRepository>();
        protected IUserTenantAppService UserTenantsAppService => LazyServiceProvider.LazyGetRequiredService<IUserTenantAppService>();

        protected static void AssignDefaultClaims(ClaimsPrincipal claimsPrinicipal, string displayName, Guid userId)
        {
            claimsPrinicipal.AddClaim("DisplayName", displayName);
            claimsPrinicipal.AddClaim("UserId", userId.ToString());
            claimsPrinicipal.AddClaim("Badge", Utils.CreateUserBadge(displayName));
        }

        protected static string? GetClaimValue(JwtSecurityToken token, string type, string? idp)
        {
            type = UnityClaimsResolver.ResolveFor(type, idp);
            return token.Claims.FirstOrDefault(s => s.Type == type)?.Value;
        }
    }
}
