using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Unity.GrantManager.GrantApplications;
using System.Linq;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

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

            List<ApplicationSectorDto> ApplicationSectors = (await _applicationSectorAppService.GetListAsync()).ToList();

            ProjectInfoViewModel model = new()
            {
                ApplicationId = applicationId,
                ApplicationSectors = ApplicationSectors
            };

            foreach (ApplicationSectorDto sector in ApplicationSectors)
            {
                model.ApplicationSectorsList.Add(new SelectListItem { Value = sector.SectorCode, Text = sector.SectorName });
            }

            if (ApplicationSectors.Count > 0 ) {
                List<ApplicationSubSectorDto>? SubSectors = new List<ApplicationSubSectorDto>();
                if(Application.SubSector.IsNullOrEmpty()) {
                    SubSectors = ApplicationSectors[0].SubSectors ?? SubSectors;
                } else {
                    ApplicationSectorDto applicationSector = ApplicationSectors.Find(x => x.SectorCode == Application.Sector) ?? throw new ArgumentException("Sector not found");
                    SubSectors = applicationSector.SubSectors;
                }
                foreach (ApplicationSubSectorDto subSector in SubSectors) {
                    model.ApplicationSubSectorsList.Add(new SelectListItem { Value = subSector.SubSectorCode, Text = subSector.SubSectorName });
                }
            }

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
                CommunityPopulation = Application.CommunityPopulation ?? 0,
                Forestry = Application.Forestry,
                ForestryFocus = Application.ForestryFocus,
                Acquisition = Application.Acquisition,
                Sector = Application.Sector,
                SubSector = Application.SubSector,
                EconomicRegion = Application.EconomicRegion,
                ElectoralDistrict = Application.ElectoralDistrict
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
