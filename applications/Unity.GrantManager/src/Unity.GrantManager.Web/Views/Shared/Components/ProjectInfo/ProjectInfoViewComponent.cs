using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Locality;
using Unity.GrantManager.Permissions;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Shared.Components.ProjectInfo;

[Widget(
    AutoInitialize = true,
    RefreshUrl = "Widget/ProjectInfo/Refresh",
    ScriptTypes = [typeof(ProjectInfoScriptBundleContributor)],
    StyleTypes = [typeof(ProjectInfoStyleBundleContributor)]
)]
public class ProjectInfoViewComponent : AbpViewComponent
{
    private readonly IGrantApplicationAppService _grantApplicationAppService;
    private readonly IEconomicRegionService _applicationEconomicRegionAppService;
    private readonly IElectoralDistrictService _applicationElectoralDistrictAppService;
    private readonly IRegionalDistrictService _applicationRegionalDistrictAppService;
    private readonly ICommunityService _applicationCommunityAppService;
    private readonly IAuthorizationService _authorizationService;

    public ProjectInfoViewComponent(
        IGrantApplicationAppService grantApplicationAppService,
        IEconomicRegionService applicationEconomicRegionAppService,
        IElectoralDistrictService applicationElectoralDistrictAppService,
        IRegionalDistrictService applicationRegionalDistrictAppService,
        ICommunityService applicationCommunityAppService,
        IAuthorizationService authorizationService)
    {
        _grantApplicationAppService = grantApplicationAppService;
        _applicationEconomicRegionAppService = applicationEconomicRegionAppService;
        _applicationElectoralDistrictAppService = applicationElectoralDistrictAppService;
        _applicationRegionalDistrictAppService = applicationRegionalDistrictAppService;
        _applicationCommunityAppService = applicationCommunityAppService;
        _authorizationService = authorizationService;
    }

    public async Task<IViewComponentResult> InvokeAsync(Guid applicationId, Guid applicationFormVersionId)
    {
        const decimal ProjectFundingMax = 10000000;
        const decimal ProjectFundingMultiply = 0.2M;
        GrantApplicationDto application = await _grantApplicationAppService.GetAsync(applicationId);

        bool finalDecisionState = GrantApplicationStateGroups.FinalDecisionStates.Contains(application.StatusCode);
        bool isFormEditGranted =  true;
            //await _authorizationService.IsGrantedAsync(GrantApplicationPermissions.ProjectInfo.Update);
        bool isEditGranted = isFormEditGranted && !finalDecisionState;
        bool isPostEditFieldsAllowed = true;
            //= isEditGranted || (await _authorizationService.IsGrantedAsync(GrantApplicationPermissions.ProjectInfo.UpdateFinalStateFields) && finalDecisionState);

        List<EconomicRegionDto> EconomicRegions = (await _applicationEconomicRegionAppService.GetListAsync()).ToList();

        List<ElectoralDistrictDto> ElectoralDistricts = (await _applicationElectoralDistrictAppService.GetListAsync()).ToList();

        List<RegionalDistrictDto> RegionalDistricts = (await _applicationRegionalDistrictAppService.GetListAsync()).ToList();

        List<CommunityDto> Communities = (await _applicationCommunityAppService.GetListAsync()).ToList();

        ProjectInfoViewModel model = new()
        {
            ApplicationId = applicationId,
            ApplicationFormId = application.ApplicationForm.Id,
            ApplicationFormVersionId = applicationFormVersionId,
            RegionalDistricts = RegionalDistricts,
            Communities = Communities,
            EconomicRegions = EconomicRegions,
            IsFinalDecisionMade = finalDecisionState,
            IsFormEditGranted = isFormEditGranted,
            IsEditGranted = isEditGranted,
            IsPostEditFieldsAllowed = isPostEditFieldsAllowed,
        };

        model.EconomicRegionList.AddRange(EconomicRegions.Select(EconomicRegion =>
            new SelectListItem { Value = EconomicRegion.EconomicRegionName, Text = EconomicRegion.EconomicRegionName }));

        model.ElectoralDistrictList.AddRange(ElectoralDistricts.Select(ElectoralDistrict =>
            new SelectListItem { Value = ElectoralDistrict.ElectoralDistrictName, Text = ElectoralDistrict.ElectoralDistrictName }));

        if (EconomicRegions.Count > 0)
        {
            String EconomicRegionCode = string.Empty;
            var economicRegionSelected = EconomicRegions.Find(x => x.EconomicRegionName == application.EconomicRegion);
            if (economicRegionSelected != null)
            {
                EconomicRegionCode = economicRegionSelected.EconomicRegionCode;
            }
            else
            {
                EconomicRegionCode = EconomicRegions[0].EconomicRegionCode;
            }
            model.RegionalDistrictList.AddRange(RegionalDistricts.FindAll(x => x.EconomicRegionCode == EconomicRegionCode).Select(RegionalDistrict =>
                new SelectListItem { Value = RegionalDistrict.RegionalDistrictName, Text = RegionalDistrict.RegionalDistrictName }));
        }

        if (RegionalDistricts.Count > 0)
        {
            String RegionalDistrictCode = string.Empty;
            var regionalDistrictSelected = RegionalDistricts.Find(x => x.RegionalDistrictName == application.RegionalDistrict);
            if (regionalDistrictSelected != null)
            {
                RegionalDistrictCode = regionalDistrictSelected.RegionalDistrictCode;
            }
            else
            {
                RegionalDistrictCode = RegionalDistricts[0].RegionalDistrictCode;
            }
            model.CommunityList.AddRange(Communities.FindAll(x => x.RegionalDistrictCode == RegionalDistrictCode).Select(community =>
                new SelectListItem { Value = community.Name, Text = community.Name }));
        }


        decimal projectFundingTotal = application.ProjectFundingTotal ?? 0;
        double percentageTotalProjectBudget = application.PercentageTotalProjectBudget ?? 0;

        if (projectFundingTotal == 0)
        {
            projectFundingTotal = decimal.Multiply(application.TotalProjectBudget, ProjectFundingMultiply);
            projectFundingTotal = (projectFundingTotal > ProjectFundingMax) ? ProjectFundingMax : projectFundingTotal;
        }

        percentageTotalProjectBudget = application.TotalProjectBudget == 0 ? 0 : decimal.Multiply(decimal.Divide(application.RequestedAmount, application.TotalProjectBudget), 100).To<double>();

        model.ProjectInfo = new()
        {
            ProjectName = application.ProjectName,
            ProjectSummary = application.ProjectSummary,
            ProjectStartDate = application.ProjectStartDate,
            ProjectEndDate = application.ProjectEndDate,
            RequestedAmount = application.RequestedAmount,
            TotalProjectBudget = application.TotalProjectBudget,
            ProjectFundingTotal = projectFundingTotal,
            PercentageTotalProjectBudget = Math.Round(percentageTotalProjectBudget, 2),
            Community = application.Community,
            CommunityPopulation = application.CommunityPopulation ?? 0,
            Forestry = application.Forestry,
            ForestryFocus = application.ForestryFocus,
            Acquisition = application.Acquisition,
            EconomicRegion = application.EconomicRegion,
            ElectoralDistrict = application.ElectoralDistrict,
            RegionalDistrict = application.RegionalDistrict,
            Place = application.Place,
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
