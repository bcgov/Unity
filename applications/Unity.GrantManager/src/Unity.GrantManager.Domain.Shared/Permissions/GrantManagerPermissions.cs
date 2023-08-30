namespace Unity.GrantManager.Permissions;

public static class GrantManagerPermissions
{
    public const string GroupName = "GrantManagerManagement";   
    
    public const string Default = GroupName + ".Default";

    public static class Organizations
    {
        public const string Default = GroupName + ".Organizations";
        public const string ManageProfiles = Default + ".ManageProfiles";
    }
}
