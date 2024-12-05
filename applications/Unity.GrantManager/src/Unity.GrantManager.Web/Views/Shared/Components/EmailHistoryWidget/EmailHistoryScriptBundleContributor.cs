using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Unity.GrantManager.Web.Views.Shared.Components.EmailHistoryWidget;

public class EmailHistoryScriptBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files
          .AddIfNotContains("/Views/Shared/Components/EmailHistoryWidget/Default.js");
        context.Files
          .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
    }
}
