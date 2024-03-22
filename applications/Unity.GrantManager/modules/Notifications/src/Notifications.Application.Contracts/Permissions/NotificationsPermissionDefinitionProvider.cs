using Notifications.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Notifications.Permissions;

public class NotificationsPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(NotificationsPermissions.GroupName, L("Permission:Notifications"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<NotificationsResource>(name);
    }
}
