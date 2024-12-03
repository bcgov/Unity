using Unity.Notifications.Localization;
using Volo.Abp.Localization;
using Volo.Abp.Settings;

namespace Unity.Notifications.Settings;

public class NotificationsSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition(
                NotificationsSettings.Mailing.DefaultFromAddress,
                string.Empty,
                L("DisplayName:Notifications.Mailing.DefaultFromAddress"),
                L("Description:Notifications.Mailing.DefaultFromAddress")),
            new SettingDefinition(
                NotificationsSettings.Mailing.DefaultFromDisplayName,
                string.Empty,
                L("DisplayName:Notifications.Mailing.DefaultFromDisplayName"),
                L("Description:Notifications.Mailing.DefaultFromDisplayName"))
            );
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<NotificationsResource>(name);
    }
}
