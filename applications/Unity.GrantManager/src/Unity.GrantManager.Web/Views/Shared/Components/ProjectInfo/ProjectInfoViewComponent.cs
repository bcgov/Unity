using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Unity.GrantManager.GrantApplications;
using System.Linq;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;

namespace Unity.GrantManager.Web.Views.Shared.Components.ProjectInfo
{

    [Widget(
        RefreshUrl = "Widget/ProjectInfo/Refresh",
        ScriptTypes = new[] { typeof(ProjectInfoScriptBundleContributor) },
        StyleTypes = new[] { typeof(ProjectInfoStyleBundleContributor) },
        AutoInitialize = true)]
    public class ProjectInfoViewComponent : AbpViewComponent
    {
        private readonly GrantApplicationAppService _grantApplicationAppService;

        public ProjectInfoViewComponent(GrantApplicationAppService grantApplicationAppService)
        {
            _grantApplicationAppService = grantApplicationAppService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
        {
            GrantApplicationDto application = await _grantApplicationAppService.GetAsync(applicationId);

            ProjectInfoViewModel model = new()
            {
                ApplicationId = applicationId
            };

            model.IsFinalDecisionMade = GrantApplicationStateGroups.FinalDecisionStates.Contains(application.StatusCode);

            model.ProjectInfo = new()
            {
                ProjectSummary = application.ProjectSummary,
                RequestedAmount = application.RequestedAmount,
                TotalProjectBudget = application.TotalProjectBudget,
            };

            return View(model);
        }
    }

    public class ProjectInfoStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ProjectInfo/Default.css");
        }
    }

    public class ProjectInfoScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ProjectInfo/Default.js");
            context.Files
              .AddIfNotContains("/libs/jquery-maskmoney/dist/jquery.maskMoney.min.js");
        }
    }
}
