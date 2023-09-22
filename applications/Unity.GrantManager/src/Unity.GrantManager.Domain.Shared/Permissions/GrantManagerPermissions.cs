namespace Unity.GrantManager.Permissions;

#pragma warning disable S3218 // Inner class members should not shadow outer class "static" or type members
public static class GrantManagerPermissions
{
    public const string GroupName = "GrantManagerManagement";   
    
    public const string Default = GroupName + ".Default";

    public static class Organizations
    {
        public const string Default = GroupName + ".Organizations";
        public const string ManageProfiles = Default + ".ManageProfiles";
    }

    public static class Intakes
    {
        public const string Default = GroupName + ".Intakes";

    }

    public static class ApplicationForms
    {
        public const string Default = GroupName + ".ApplicationForms";
    }
}
#pragma warning restore S3218 // Inner class members should not shadow outer class "static" or type members