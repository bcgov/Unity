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
            context.Files
              .AddIfNotContains("/Pages/BulkApprovals/ApproveApplicationsModal.css");
            context.Files
              .AddIfNotContains("/Pages/BulkActions/BulkPublishApplications.css");
            context.Files
              .AddIfNotContains("/Pages/BulkEmailNotifications/SendEmailNotificationModal.css");
            context.Files
              .AddIfNotContains("/Pages/BulkEmailNotifications/ComposeAndSendEmailModal.css");
            // The bulk-send modal mounts EmailsWidget dynamically (via abp.WidgetManager) for the selected
            // application, rather than server-rendering it as part of this page — so its own [Widget] bundle
            // never gets a chance to auto-register here. Pull it in explicitly.
            context.Files
              .AddIfNotContains("/Views/Shared/Components/EmailsWidget/Default.css");
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
            // NOTE: CreateHistoricalPaymentsModal.js depends on helpers defined in CreatePaymentRequestsModal.js — must remain after it.
            context.Files
              .AddIfNotContains("/Pages/PaymentRequests/CreateHistoricalPaymentsModal.js");
            context.Files
              .AddIfNotContains("/libs/jquery-maskmoney/dist/jquery.maskMoney.min.js");
            context.Files
              .AddIfNotContains("/Pages/BulkApprovals/ApproveApplicationsModal.js");
            context.Files
              .AddIfNotContains("/Pages/BulkActions/BulkPublishApplications.js");
            context.Files
              .AddIfNotContains("/Pages/BulkEmailNotifications/SendEmailNotificationModal.js");
            // Same reasoning as the style bundle above: EmailsWidget is only ever mounted dynamically inside
            // the bulk-send modal on this page, so its script needs to be pulled in explicitly rather than
            // relying on the widget's own [Widget(AutoInitialize = true)] bundle registration, which only
            // fires for widgets that are actually server-rendered as part of a page's initial HTML.
            context.Files
              .AddIfNotContains("/Views/Shared/Components/EmailsWidget/Default.js");
            context.Files
              .AddIfNotContains("/Pages/BulkEmailNotifications/ComposeAndSendEmailModal.js");
        }
    }
}

