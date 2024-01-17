using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationTagsWidget
{
    [Widget(
        RefreshUrl = "Widgets/Tags/RefreshTags",
        ScriptTypes = new[] { typeof(ApplicationTagsWidgetScriptBundleContributor) },
        StyleTypes = new[] { typeof(ApplicationTagsWidgetStyleBundleContributor) },
        AutoInitialize = true)]
    public class ApplicationTagsWidgetViewComponent : AbpViewComponent
    {
        private readonly IApplicationTagsService _applicationTagsService;

        public ApplicationTagsWidgetViewComponent(IApplicationTagsService applicatioTagsService)
        {
            _applicationTagsService = applicatioTagsService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
        {
            var applicationTags = await _applicationTagsService.GetApplicationTagsAsync(applicationId);
            string applicationText = "";
            if(applicationTags != null)
            {
                applicationText = applicationTags.Text;
            }
        
            return View(new ApplicationTagsWidgetViewModel() { ApplicationTags = applicationText.Replace(",", ", ")});
        }
    }

    public class ApplicationTagsWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ApplicationTagsWidget/Default.css");
        }
    }

    public class ApplicationTagsWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ApplicationTagsWidget/Default.js");
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}
