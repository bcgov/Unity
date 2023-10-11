using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationAttachments
{

    [Widget(
        ScriptTypes = new[] { typeof(ApplicationAttachmentsScriptBundleContributor) },
        StyleTypes = new[] { typeof(ApplicationAttachmentsStyleBundleContributor) })]
    public class ApplicationAttachments : AbpViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }

    public class ApplicationAttachmentsStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/_Shared/Attachments.css");
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ApplicationAttachments/ApplicationAttachments.css");
        }
    }

    public class ApplicationAttachmentsScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/_Shared/Attachments.js");
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ApplicationAttachments/ApplicationAttachments.js");
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}
