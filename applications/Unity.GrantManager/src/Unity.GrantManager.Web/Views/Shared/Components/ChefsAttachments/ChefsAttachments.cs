using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Volo.Abp.Features;

namespace Unity.GrantManager.Web.Views.Shared.Components.ChefsAttachments
{

    [Widget(
        ScriptTypes = new[] { typeof(ChefsAttachmentsScriptBundleContributor) },
        StyleTypes = new[] { typeof(ChefsAttachmentsStyleBundleContributor) })]
    public class ChefsAttachments : AbpViewComponent
    {
        private readonly IFeatureChecker _featureChecker;

        public ChefsAttachments(IFeatureChecker featureChecker)
        {
            _featureChecker = featureChecker;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var isAIAttachmentSummariesEnabled = await _featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries");
            ViewBag.IsAIAttachmentSummariesEnabled = isAIAttachmentSummariesEnabled;
            return View();
        }
    }

    public class ChefsAttachmentsStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ChefsAttachments/ChefsAttachments.css");
        }
    }

    public class ChefsAttachmentsScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ChefsAttachments/ChefsAttachments.js");
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}
