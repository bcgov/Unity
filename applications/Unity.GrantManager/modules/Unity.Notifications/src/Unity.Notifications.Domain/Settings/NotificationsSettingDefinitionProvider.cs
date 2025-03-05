using System.Collections.Generic;
using Unity.Notifications.Localization;
using Volo.Abp.Localization;
using Volo.Abp.Settings;

namespace Unity.Notifications.Settings;

public class NotificationsSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        var notificationsSettings = new Dictionary<string, string>
        {
            { NotificationsSettings.Mailing.DefaultFromAddress, "NoReply.Unity@gov.bc.ca"},
            { NotificationsSettings.Mailing.EmailMaxRetryAttempts, "3"}
        };

        foreach (var notificationSetting in notificationsSettings)
        {
            AddSettingDefinition(context, notificationSetting.Key, notificationSetting.Value.ToString());
        }
    }
    
    private static void AddSettingDefinition(ISettingDefinitionContext currentContext, string settingName, string defaultValue = "True")
    {
        var displayName = L($"Setting:{settingName}.DisplayName");
        var description = L($"Setting:{settingName}.Description");

        currentContext.Add(
            new SettingDefinition(
                settingName,
                defaultValue,
                displayName,
                description,
                isVisibleToClients: true,
                isInherited: false,
                isEncrypted: false)
        );
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<NotificationsResource>(name);
    }
}
