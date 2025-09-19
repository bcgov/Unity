using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantsActionBar
{
    [Widget(
        ScriptTypes = [typeof(ApplicantsActionBarWidgetScriptBundleContributor)],
        StyleTypes = [typeof(ApplicantsActionBarWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class ApplicantsActionBar : AbpViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }

    public class ApplicantsActionBarWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ApplicantsActionBar/Default.css");
        }
    }

    public class ApplicantsActionBarWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ApplicantsActionBar/Default.js");
        }
    }
}