using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Volo.Abp.AspNetCore.Authentication.OAuth.Claims;

namespace Unity.GrantManager.Web.Identity
{
    public static class ClaimActionCollectionExtensions
    {
        public static void MapClaimTypes(this ClaimActionCollection claimActions)
        {
            // Do any custom mapping here
        }

        public static void MapJsonKeyMultiple(this ClaimActionCollection claimActions, string claimType, string jsonKey)
        {
            claimActions.Add(new MultipleClaimAction(claimType, jsonKey));
        }

        public static void RemoveDuplicate(this ClaimActionCollection claimActions, string claimType)
        {
            claimActions.Add(new RemoveDuplicateClaimAction(claimType));
        }
    }
}
