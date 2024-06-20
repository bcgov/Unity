using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.Payments.Web.Views.Shared.Components.ActionBar
{
    [Widget(
        RefreshUrl = "Widget/PaymentActionBar/Refresh",
        ScriptTypes = [typeof(PaymentActionBarWidgetScriptBundleContributor)],
        StyleTypes = [typeof(PaymentActionBarWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class PaymentActionBar : AbpViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }

    public class PaymentActionBarWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/PaymentActionBar/Default.css");
        }
    }

    public class PaymentActionBarWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/PaymentActionBar/Default.js");
        }
    }
}

