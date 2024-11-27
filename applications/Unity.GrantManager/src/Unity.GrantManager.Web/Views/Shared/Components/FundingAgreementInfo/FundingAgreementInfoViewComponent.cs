using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

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

        public FundingAgreementInfoViewComponent(
            IGrantApplicationAppService grantApplicationAppService)
        {
            _grantApplicationAppService = grantApplicationAppService;

        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId, Guid applicationFormVersionId)
        {

            GrantApplicationDto application = await _grantApplicationAppService.GetAsync(applicationId);

            FundingAgreementInfoViewModel model = new()
            {
                ApplicationId = applicationId,
                ApplicationFormId = application.ApplicationForm.Id,
                ApplicationFormVersionId = applicationFormVersionId,
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
