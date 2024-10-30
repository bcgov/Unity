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
using Unity.GrantManager.Locality;
using Unity.GrantManager.Permissions;
using Microsoft.AspNetCore.Authorization;

namespace Unity.GrantManager.Web.Views.Shared.Components.FundingAgreementInfo
{

    [Widget(
        RefreshUrl = "Widget/FundingAgreementInfo/Refresh",
        ScriptTypes = [typeof(FundingAgreementInfoScriptBundleContributor)],
        StyleTypes = [typeof(FundingAgreementInfoStyleBundleContributor)],
        AutoInitialize = true)]
    public class FundingAgreementInfoViewComponent : AbpViewComponent
    {
        private readonly IGrantApplicationAppService _grantApplicationAppService;
        private readonly IEconomicRegionService _applicationEconomicRegionAppService;
        private readonly IElectoralDistrictService _applicationElectoralDistrictAppService;
        private readonly IRegionalDistrictService _applicationRegionalDistrictAppService;
        private readonly ICommunityService _applicationCommunityAppService;
        private readonly IAuthorizationService _authorizationService;        

        public FundingAgreementInfoViewComponent(
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
            //const decimal ProjectFundingMax = 10000000;
            //const decimal ProjectFundingMultiply = 0.2M;
            GrantApplicationDto application = await _grantApplicationAppService.GetAsync(applicationId);

            //bool finalDecisionState = GrantApplicationStateGroups.FinalDecisionStates.Contains(application.StatusCode);
            //bool isEditGranted = await _authorizationService.IsGrantedAsync(GrantApplicationPermissions.AssessmentResults.Edit) && !finalDecisionState;
            //bool isPostEditFieldsAllowed = isEditGranted || (await _authorizationService.IsGrantedAsync(GrantApplicationPermissions.AssessmentResults.EditFinalStateFields) && finalDecisionState);

            //List<EconomicRegionDto> EconomicRegions = (await _applicationEconomicRegionAppService.GetListAsync()).ToList();

            //List<ElectoralDistrictDto> ElectoralDistricts = (await _applicationElectoralDistrictAppService.GetListAsync()).ToList();

            //List<RegionalDistrictDto> RegionalDistricts = (await _applicationRegionalDistrictAppService.GetListAsync()).ToList();

            //List<CommunityDto> Communities = (await _applicationCommunityAppService.GetListAsync()).ToList();

            FundingAgreementInfoViewModel model = new()
            {
                ApplicationId = applicationId,
                ApplicationFormId = application.ApplicationForm.Id,
                ApplicationFormVersionId = applicationFormVersionId,
                //RegionalDistricts = RegionalDistricts,
                //Communities = Communities,
                //EconomicRegions = EconomicRegions,
                //IsFinalDecisionMade = finalDecisionState,
                //IsEditGranted = isEditGranted,
                //IsPostEditFieldsAllowed = isPostEditFieldsAllowed,
            };

            model.FundingAgreementInfo = new()
            {
                ContractNumber = application.ContractNumber,
                ContractExecutionDate = application.ContractExecutionDate,            
            };

            return View(model);
        }
    }

    public class FundingAgreementInfoStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/FundingAgreementInfo/Default.css");
        }
    }

    public class FundingAgreementInfoScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/FundingAgreementInfo/Default.js");
            context.Files
              .AddIfNotContains("/libs/jquery-maskmoney/dist/jquery.maskMoney.min.js");
        }
    }
}
