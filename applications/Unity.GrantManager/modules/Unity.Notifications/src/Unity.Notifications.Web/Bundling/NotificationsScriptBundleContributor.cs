using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Unity.Notifications.Web.Bundling;

public class NotificationsScriptBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.AddIfNotContains("/libs/select2/dist/js/select2.full.js");
        context.Files.AddIfNotContains("/libs/signalr/browser/signalr.min.js");
    }
}
