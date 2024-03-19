namespace Unity.GrantManager.Web.Identity;

public static class UnityClaimsTypes
{
    public const string Role = "client_roles";
    public const string PreferredUsername = "preferred_username";
    public const string GivenName = "given_name";   
    public const string FamilyName = "family_name";
    public const string Email = "email";

    public const string IDirUsername = "idir_username";
    public const string DisplayName = "display_name";
    public const string EmailVerified = "email_verified";
    public const string IDirUserGuid = "idir_user_guid";
    public const string Subject = "sub";

    public const string Permission = "Permission";
    public const string IdpProvider  = "identity_provider";
    public const string Tenant = "tenant";
}
