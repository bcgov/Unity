using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Shared.Components.ActionBar
{
    [Widget(        
        ScriptTypes = [typeof(ActionBarWidgetScriptBundleContributor)],
        StyleTypes = [typeof(ActionBarWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class ActionBar : AbpViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }

    public class ActionBarWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ActionBar/Default.css");
        }
    }

    public class ActionBarWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ActionBar/Default.js");
            context.Files
              .AddIfNotContains("/Pages/ApplicationTags/ApplicationTags.js");
            context.Files
              .AddIfNotContains("/Pages/AssigneeSelection/AssigneeSelection.js");
            context.Files
              .AddIfNotContains("/Pages/PaymentRequests/CreatePaymentRequestsModal.js");
            context.Files
              .AddIfNotContains("/libs/jquery-maskmoney/dist/jquery.maskMoney.min.js");
            context.Files
            .AddIfNotContains("/Pages/PaymentApprovals/UpdatePaymentRequestStatusModal.js");
        }
    }
}

