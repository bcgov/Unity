using System;

namespace Unity.GrantManager.Web.Identity
{
    public static class UnityClaimsResolver
    {
        /// <summary>
        /// Provide the claim type and the identity provider and resolver for this combination
        /// </summary>
        /// <param name="claimType"></param>
        /// <param name="idp"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static string ResolveFor(string claimType, string? idp)
        {
            if ((idp == "idir" || idp == "azureidir") && claimType == UnityClaimsTypes.PreferredUsername) 
            {
                return UnityClaimsTypes.IDirUsername;
            }        
            
            return claimType;                        
        }
    }
}
