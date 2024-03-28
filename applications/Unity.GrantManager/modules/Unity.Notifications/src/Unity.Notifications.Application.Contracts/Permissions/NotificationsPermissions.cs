using Volo.Abp.Reflection;

namespace Unity.Notifications.Permissions;

public static class NotificationsPermissions
{
    public const string GroupName = "Notifications";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(NotificationsPermissions));
    }
}
