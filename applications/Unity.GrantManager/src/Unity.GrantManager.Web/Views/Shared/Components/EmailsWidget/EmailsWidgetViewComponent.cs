using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Microsoft.Extensions.Configuration;

namespace Unity.GrantManager.Web.Views.Shared.Components.EmailsWidget
{
    [Widget(
        RefreshUrl = "Widgets/Emails/RefreshEmails",
        ScriptTypes = [typeof( EmailsWidgetScriptBundleContributor)],
        StyleTypes = [typeof( EmailsWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class EmailsWidgetViewComponent(IConfiguration configuration) : AbpViewComponent
    {
        public IViewComponentResult Invoke(Guid ownerId, Guid currentUserId)
        {
            EmailsWidgetViewModel model = new()
            {
                OwnerId = ownerId,
                CurrentUserId = currentUserId,
                EmailFrom = configuration["Notifications:ChesFromEmail"] ?? "unity@gov.bc.ca",
            };

            return View(model);
        }
    }

    public class  EmailsWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files.AddIfNotContains("/Views/Shared/Components/EmailsWidget/Default.css");
        }
    }

    public class  EmailsWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files.AddIfNotContains("/Views/Shared/Components/EmailsWidget/Default.js");
            context.Files.AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}
