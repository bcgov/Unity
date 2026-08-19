using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Unity.Notifications.Web.Bundling;

public class NotificationsScriptBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.AddIfNotContains("/libs/signalr/browser/signalr.js");
        context.Files.AddIfNotContains("/js/notifications-realtime-client.js");
    }
}
