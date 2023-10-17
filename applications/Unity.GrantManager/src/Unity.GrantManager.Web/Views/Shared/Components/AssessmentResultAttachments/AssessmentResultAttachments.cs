using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;

namespace Unity.GrantManager.Web.Views.Shared.Components.AssessmentResultAttachments
{

    [Widget(
        ScriptTypes = new[] { typeof(AssessmentResultAttachmentsScriptBundleContributor) },
        StyleTypes = new[] { typeof(AssessmentResultAttachmentsStyleBundleContributor) })]
    public class AssessmentResultAttachments : AbpViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }

    public class AssessmentResultAttachmentsStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/_Shared/Attachments.css");
            context.Files
              .AddIfNotContains("/Views/Shared/Components/AssessmentResultAttachments/AssessmentResultAttachments.css");
        }
    }

    public class AssessmentResultAttachmentsScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/_Shared/Attachments.js");
            context.Files
              .AddIfNotContains("/Views/Shared/Components/AssessmentResultAttachments/AssessmentResultAttachments.js");
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}
