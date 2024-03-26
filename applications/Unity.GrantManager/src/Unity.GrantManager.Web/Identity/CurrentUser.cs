using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Security.Claims;
using Volo.Abp.Users;

namespace Unity.GrantManager.Web.Identity
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(ICurrentUser))]
    public class CurrentUser : ICurrentUser, ITransientDependency
    {
        private static readonly Claim[] EmptyClaimsArray = Array.Empty<Claim>();

        public virtual bool IsAuthenticated => Id.HasValue;

        public virtual Guid? Id => this.FindUserId();

        public virtual string? UserName => this.FindClaimValue(UnityClaimsTypes.PreferredUsername);

        public virtual string? Name => this.FindClaimValue(ClaimTypes.GivenName);

        public virtual string? SurName => this.FindClaimValue(ClaimTypes.Surname);

        public virtual string? PhoneNumber => this.FindClaimValue(AbpClaimTypes.PhoneNumber);

        public virtual bool PhoneNumberVerified => string.Equals(this.FindClaimValue(AbpClaimTypes.PhoneNumberVerified), "true", StringComparison.InvariantCultureIgnoreCase);

        public virtual string? Email => this.FindClaimValue(ClaimTypes.Email);

        public virtual bool EmailVerified => string.Equals(this.FindClaimValue(AbpClaimTypes.EmailVerified), "true", StringComparison.InvariantCultureIgnoreCase);

        public virtual Guid? TenantId => _principalAccessor.Principal?.FindTenantId();

        public virtual string[] Roles => FindRoleClaims();

        private string[] FindRoleClaims()
        {
            return FindClaims(UnityClaimsTypes.Role).Select(c => c.Value).Distinct().ToArray();
        }

        private readonly ICurrentPrincipalAccessor _principalAccessor;

        public CurrentUser(ICurrentPrincipalAccessor principalAccessor)
        {
            _principalAccessor = principalAccessor;
        }

        public virtual Claim? FindClaim(string claimType)
        {
            return _principalAccessor.Principal?.Claims.FirstOrDefault(c => c.Type == claimType);
        }

        public virtual Claim[] FindClaims(string claimType)
        {
            return _principalAccessor.Principal?.Claims.Where(c => c.Type == claimType).ToArray() ?? EmptyClaimsArray;
        }

        public virtual Claim[] GetAllClaims()
        {
            return _principalAccessor.Principal?.Claims.ToArray() ?? EmptyClaimsArray;
        }

        public virtual bool IsInRole(string roleName)
        {
            return FindClaims(UnityClaimsTypes.Role)
                .ToList()
                .Exists(c => c.Value == roleName);
        }

        public virtual Guid? FindUserId()
        {
            var userClaims = _principalAccessor.Principal?.Claims;
            if (userClaims != null && userClaims.Any())
            {
                var userId = userClaims.FirstOrDefault(s => s.Type == "UserId");
                if (userId != null)
                {
                    return Guid.Parse(userId.Value);
                }
            }
            return null;
        }
    }
}
