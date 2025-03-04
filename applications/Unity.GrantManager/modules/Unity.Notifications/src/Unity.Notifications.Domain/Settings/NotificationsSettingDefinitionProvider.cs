﻿using Unity.Notifications.Localization;
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
                "NoReply.Unity@gov.bc.ca",
                L($"Setting:{NotificationsSettings.Mailing.DefaultFromAddress}.DisplayName"),
                L($"Setting:{NotificationsSettings.Mailing.DefaultFromAddress}.Description"),
                isVisibleToClients: false,
                isInherited: false)
            );
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<NotificationsResource>(name);
    }
}
