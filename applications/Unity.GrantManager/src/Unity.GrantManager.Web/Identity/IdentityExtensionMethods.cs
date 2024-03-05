using OpenIddict.Abstractions;
using System.Collections.Immutable;
using System.Security.Claims;

namespace Unity.GrantManager.Web.Identity
{
    public static class IdentityExtensionMethods
    {
        public static ClaimsPrincipal AddPermission(this ClaimsPrincipal principal, string value) 
        {
            return principal.AddClaim(UnityClaimsTypes.Permission, value);            
        }

        public static ClaimsPrincipal AddPermissions(this ClaimsPrincipal principal, ImmutableArray<string> values)
        {
            return principal.AddClaims(UnityClaimsTypes.Permission, values);
        }
    }
}
