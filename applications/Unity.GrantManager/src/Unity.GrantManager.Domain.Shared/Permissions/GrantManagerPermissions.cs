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
    
    public static class Endpoints
    {
        public const string Default = GroupName + ".Endpoints";
        public const string ManageEndpoints = Default + ".ManageEndpoints";
    }

    /// <summary>
    /// Permission constants for the generic contacts service.
    /// These are pre-wired for future HTTP endpoint exposure.
    /// </summary>
    public static class Contacts
    {
        public const string Default = GroupName + ".Contacts";
        public const string Create = Default + ".Create";
        public const string Read = Default + ".Read";
        public const string Update = Default + ".Update";
    }
}
#pragma warning restore S3218 // Inner class members should not shadow outer class "static" or type members