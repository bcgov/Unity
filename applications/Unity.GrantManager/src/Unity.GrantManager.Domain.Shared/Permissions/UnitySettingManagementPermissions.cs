namespace Unity.GrantManager.Permissions;
public static class UnitySettingManagementPermissions
{
    public const string GroupName = "SettingManagement";

    public const string UserInterface = GroupName + ".UserInterface";

    public const string BackgroundJobSettings = "SettingManagement.GrantManager";

    public static class Tags
    {
        public const string Default = "Unity.GrantManager.SettingManagement.Tags";
        public const string Create  = "Unity.GrantManager.SettingManagement.Tags.Create";
        public const string Update  = "Unity.GrantManager.SettingManagement.Tags.Update";
        public const string Delete  = "Unity.GrantManager.SettingManagement.Tags.Delete";
    }
}
