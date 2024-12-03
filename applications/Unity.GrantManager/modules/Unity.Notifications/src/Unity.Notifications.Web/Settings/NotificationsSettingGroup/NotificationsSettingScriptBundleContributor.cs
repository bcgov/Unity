using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Unity.Notifications.Web.Settings.NotificationsSettingGroup;

public class NotificationsSettingScriptBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.AddIfNotContains("/Settings/NotificationsSettingGroup/Default.js");
    }
}
