using Volo.Abp.Reflection;

namespace Notifications.Permissions;

public class NotificationsPermissions
{
    public const string GroupName = "Notifications";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(NotificationsPermissions));
    }
}
