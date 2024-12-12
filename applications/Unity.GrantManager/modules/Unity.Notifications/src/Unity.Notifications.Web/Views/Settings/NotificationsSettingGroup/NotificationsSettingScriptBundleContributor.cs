using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Unity.Notifications.Web.Views.Settings.NotificationsSettingGroup;

public class NotificationsSettingScriptBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.AddIfNotContains("/Views/Settings/NotificationsSettingGroup/Default.js");
    }
}
