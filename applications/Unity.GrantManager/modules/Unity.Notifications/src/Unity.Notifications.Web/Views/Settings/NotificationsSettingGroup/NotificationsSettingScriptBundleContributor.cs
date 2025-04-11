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

public class NotificationsSettingStyleBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.AddIfNotContains("/libs/tinymce/skins/ui/oxide/content.css");
        context.Files.AddIfNotContains("/libs/tinymce/skins/content/default/content.css");
        context.Files.AddIfNotContains("/libs/tinymce/skins/ui/oxide/skin.css");

        context.Files
          .AddIfNotContains("/Views/Settings/NotificationsSettingGroup/Default.css");
    }
}