using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.Payments.Web.Views.Shared.Components.BatchPaymentActionBar
{
    [Widget(
        RefreshUrl = "Widget/BatchPaymentActionBar/Refresh",
        ScriptTypes = [typeof(BatchPaymentActionBarWidgetScriptBundleContributor)],
        StyleTypes = [typeof(BatchPaymentActionBarWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class BatchPaymentActionBar : AbpViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }

    public class BatchPaymentActionBarWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/PaymentActionBar/Default.css");
        }
    }

    public class BatchPaymentActionBarWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/PaymentActionBar/Default.js");
        }
    }
}

