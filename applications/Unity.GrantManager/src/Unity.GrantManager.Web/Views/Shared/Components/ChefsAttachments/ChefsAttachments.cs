using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.Features;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Settings;
using Unity.AI.Permissions;
using Unity.AI.Settings;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.Web.Views.Shared.Components.ChefsAttachments
{

    [Widget(
        ScriptTypes = new[] { typeof(ChefsAttachmentsScriptBundleContributor) },
        StyleTypes = new[] { typeof(ChefsAttachmentsStyleBundleContributor) })]
    public class ChefsAttachments(
        IFeatureChecker featureChecker,
        IPermissionChecker permissionChecker,
        IApplicationFormRepository applicationFormRepository) : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(Guid applicationFormId)
            {
                // Set safe defaults so the view is never left with null ViewBag values
                // even if an exception is thrown partway through the checks below.
                ViewBag.IsAIAttachmentSummariesEnabled = false;
                ViewBag.IsAIAttachmentSummariesGenerateEnabled = false;

                var featureEnabled = await featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries");

                // View guard — for toggling visibility of existing summaries
                ViewBag.IsAIAttachmentSummariesEnabled =
                    featureEnabled &&
                    await permissionChecker.IsGrantedAsync(AIPermissions.Analysis.ViewAttachmentSummary);

                if (applicationFormId == Guid.Empty)
                {
                    return View();
                }

                // Generate guard — full 3-level chain for the Generate Summary button
                var settingProvider = LazyServiceProvider.LazyGetRequiredService<ISettingProvider>();
                var tenantManualEnabled = await settingProvider.GetAsync<bool>(AISettings.ManualGenerationEnabled, defaultValue: false);
                var applicationForm = await applicationFormRepository.GetAsync(applicationFormId);

                ViewBag.IsAIAttachmentSummariesGenerateEnabled =
                    featureEnabled &&
                    tenantManualEnabled &&
                    applicationForm.ManuallyInitiateAIAnalysis &&
                    await permissionChecker.IsGrantedAsync(AIPermissions.Analysis.GenerateAttachmentSummaries);

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
