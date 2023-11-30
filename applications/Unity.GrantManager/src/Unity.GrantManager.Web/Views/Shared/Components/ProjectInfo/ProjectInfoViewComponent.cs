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
        private readonly ApplicationSectorAppService _applicationSectorAppService;

        public ProjectInfoViewComponent(GrantApplicationAppService grantApplicationAppService, ApplicationSectorAppService applicationSectorAppService)
        {
            _grantApplicationAppService = grantApplicationAppService;
            _applicationSectorAppService = applicationSectorAppService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
        {
            const decimal ProjectFundingMax = 10000000;
            const decimal ProjectFundingMultiply = 0.2M;
            GrantApplicationDto Application = await _grantApplicationAppService.GetAsync(applicationId);

            // TODO: Map the sector and subsector list to the dropdowns
            // TODO: Map using sector code from inliatziing value

            List<ApplicationSectorDto> ApplicationSectors = (await _applicationSectorAppService.GetListAsync()).ToList();

            ProjectInfoViewModel model = new()
            {
                ApplicationId = applicationId
            };

            decimal ProjectFundingTotal = Application.ProjectFundingTotal ?? 0;
            double PercentageTotalProjectBudget = Application.PercentageTotalProjectBudget ?? 0;
            if(ProjectFundingTotal == 0) {
                ProjectFundingTotal = decimal.Multiply(Application.TotalProjectBudget, ProjectFundingMultiply);
                ProjectFundingTotal = (ProjectFundingTotal > ProjectFundingMax) ? ProjectFundingMax : ProjectFundingTotal;
            }
            if(PercentageTotalProjectBudget == 0) {
                PercentageTotalProjectBudget = decimal.Divide(Application.RequestedAmount, Application.TotalProjectBudget).To<double>();
            }

            model.IsFinalDecisionMade = GrantApplicationStateGroups.FinalDecisionStates.Contains(Application.StatusCode);

            model.ProjectInfo = new()
            {
                ProjectName = Application.ProjectName,
                ProjectSummary = Application.ProjectSummary,
                ProjectStartDate = Application.ProjectStartDate,
                ProjectEndDate = Application.ProjectEndDate,
                RequestedAmount = Application.RequestedAmount,
                TotalProjectBudget = Application.TotalProjectBudget,
                ProjectFundingTotal = ProjectFundingTotal,
                PercentageTotalProjectBudget = Math.Round(PercentageTotalProjectBudget, 2),
                Community = Application.Community,
                CommunityPopulation = Application.CommunityPopulation,
                Forestry = Application.Forestry,
                ForestryFocus = Application.ForestryFocus,
                Acquisition = Application.Acquisition,
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
