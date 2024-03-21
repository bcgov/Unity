using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationContactsWidget
{
    [Widget(
        RefreshUrl = "Widgets/ApplicationContacts/RefreshApplicationContacts",
        ScriptTypes = new[] { typeof(ApplicationContactsWidgetScriptBundleContributor) },
        StyleTypes = new[] { typeof(ApplicationContactsWidgetStyleBundleContributor) },
        AutoInitialize = true)]
    public class ApplicationContactsWidgetViewComponent : AbpViewComponent
    {
        private readonly IApplicationContactService _applicationContactService;

        public ApplicationContactsWidgetViewComponent(IApplicationContactService applicationContactService)
        {
            _applicationContactService = applicationContactService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId, Boolean isReadOnly)
        {
            List<ApplicationContactDto> applicationContacts = await _applicationContactService.GetListByApplicationAsync(applicationId);
            ApplicationContactsWidgetViewModel model = new() {
                ApplicationContacts = applicationContacts,
                ApplicationId = applicationId,
                IsReadOnly = isReadOnly
            };

            return View(model);
        }
    }

    public class ApplicationContactsWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ApplicationContactsWidget/Default.css");
        }
    }

    public class ApplicationContactsWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ApplicationContactsWidget/Default.js");
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}
