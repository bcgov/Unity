using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantAttachments
{

    [Widget(
        ScriptTypes = [typeof(ApplicantAttachmentsScriptBundleContributor)],
        StyleTypes = [typeof(ApplicantAttachmentsStyleBundleContributor)])]
    public class ApplicantAttachments : AbpViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }

    public class ApplicantAttachmentsStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/_Shared/Attachments.css");
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ApplicantAttachments/ApplicantAttachments.css");
        }
    }

    public class ApplicantAttachmentsScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/_Shared/Attachments.js");
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ApplicantAttachments/ApplicantAttachments.js");
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}
