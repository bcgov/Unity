using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Volo.Abp.Features;
using Volo.Abp.Authorization.Permissions;
using Unity.AI.Permissions;

namespace Unity.GrantManager.Web.Views.Shared.Components.ChefsAttachments
{

    [Widget(
        ScriptTypes = new[] { typeof(ChefsAttachmentsScriptBundleContributor) },
        StyleTypes = new[] { typeof(ChefsAttachmentsStyleBundleContributor) })]
    public class ChefsAttachments : AbpViewComponent
    {
        private readonly IFeatureChecker _featureChecker;
        private readonly IPermissionChecker _permissionChecker;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        public ChefsAttachments(
            IFeatureChecker featureChecker,
            IPermissionChecker permissionChecker,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration)
        {
            _featureChecker = featureChecker;
            _permissionChecker = permissionChecker;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var isAIAttachmentSummariesEnabled =
                await _featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries") &&
                await _permissionChecker.IsGrantedAsync(AIPermissions.AttachmentSummary.AttachmentSummaryDefault);
            ViewBag.IsAIAttachmentSummariesEnabled = isAIAttachmentSummariesEnabled;
            ViewBag.IsDevPromptControlsEnabled = _webHostEnvironment.IsDevelopment();
            ViewBag.DefaultPromptVersion = string.IsNullOrWhiteSpace(_configuration["Azure:OpenAI:PromptVersion"])
                ? "v1"
                : _configuration["Azure:OpenAI:PromptVersion"]!.Trim().ToLowerInvariant();
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

