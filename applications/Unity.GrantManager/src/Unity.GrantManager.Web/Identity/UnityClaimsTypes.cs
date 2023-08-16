using System.Security.Claims;

namespace Unity.GrantManager.Web.Identity
{
    public class UnityClaimsTypes
    {
        //public const string Role = "client_roles"; // Pathfinder SSO
        public const string Role = ClaimTypes.Role; // Local keycloak
        public const string Username = "preferred_username";
    }
}
