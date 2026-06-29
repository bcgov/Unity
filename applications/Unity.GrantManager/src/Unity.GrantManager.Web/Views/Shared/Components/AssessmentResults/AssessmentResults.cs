using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Zones;
using Unity.Modules.Shared;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.Authorization.Permissions;

namespace Unity.GrantManager.Web.Views.Shared.Components.AssessmentResults
{

    [Widget(
        RefreshUrl = "Widget/AssessmentResults/Refresh",
        ScriptTypes = [typeof(AssessmentResultsScriptBundleContributor)],
        StyleTypes = [typeof(AssessmentResultsStyleBundleContributor)],
        AutoInitialize = true)]
    public class AssessmentResults : AbpViewComponent
    {
        private readonly GrantApplicationAppService _grantApplicationAppService;
        private readonly IAuthorizationService _authorizationService;

        protected readonly IPermissionChecker _permissionChecker;
        public readonly IZoneManagementAppService _zoneManagementAppService;

        public AssessmentResults(
            GrantApplicationAppService grantApplicationAppService,
            IAuthorizationService authorizationService,
            IPermissionChecker permissionChecker,
            IZoneManagementAppService zoneManagementAppService)
        {
            _grantApplicationAppService = grantApplicationAppService;
            _authorizationService = authorizationService;
            _permissionChecker = permissionChecker;
            _zoneManagementAppService = zoneManagementAppService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId, Guid applicationFormVersionId)
        {
            GrantApplicationDto application = await _grantApplicationAppService.GetAsync(applicationId);
            bool finalDecisionState = GrantApplicationStateGroups.FinalDecisionStates.Contains(application.StatusCode);
            bool isFormEditGranted = await _authorizationService.IsGrantedAsync(UnitySelector.Review.AssessmentResults.Update.Default);
            bool isEditGranted = isFormEditGranted && !finalDecisionState;

            AssessmentResultsPageModel model = new()
            {
                ApplicationId = applicationId,
                ApplicationFormId = application.ApplicationForm.Id,
                ApplicationFormVersionId = applicationFormVersionId,
                IsFormEditGranted = isFormEditGranted,
                IsEditGranted = isEditGranted
            };
            
            model.IsPostEditFieldsAllowed_Approval = isEditGranted || (await _authorizationService.IsGrantedAsync(UnitySelector.Review.Approval.Update.UpdateFinalStateFields) && finalDecisionState);
            model.IsPostEditFieldsAllowed_AssessmentResults = isEditGranted || (await _authorizationService.IsGrantedAsync(UnitySelector.Review.AssessmentResults.Update.UpdateFinalStateFields) && finalDecisionState);

            model.ZoneStateSet = await _zoneManagementAppService.GetZoneStateSetAsync(application.ApplicationForm.Id);

            if (model.ZoneStateSet.Contains(UnitySelector.Review.Approval.Default))
            {
                model.ApprovalView = new()
                {
                    ApprovedAmount = application.ApprovedAmount,
                    SubStatus = application.SubStatus,
                    FinalDecisionDate = application.FinalDecisionDate,
                    Notes = application.Notes
                };
            }

            if (model.ZoneStateSet.Contains(UnitySelector.Review.AssessmentResults.Default))
            {
                model.AssessmentResultsView = new()
                {
                    RequestedAmount = application.RequestedAmount,
                    TotalProjectBudget = application.TotalProjectBudget,
                    RecommendedAmount = application.RecommendedAmount,
                    LikelihoodOfFunding = application.LikelihoodOfFunding,
                    RiskRanking = application.RiskRanking,
                    DueDiligenceStatus = application.DueDiligenceStatus,
                    TotalScore = application.TotalScore,
                    AssessmentResultStatus = application.AssessmentResultStatus,
                    DeclineRational = application.DeclineRational,
                    NotificationDate = application.NotificationDate,
                    DueDate = application.DueDate,
                    ProjectSummary = application.ProjectSummary,
                };
            }

            return View(model);
        }
    }

    public class AssessmentResultsStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/AssessmentResults/Default.css");
        }
    }

    public class AssessmentResultsScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/AssessmentResults/Default.js");
            context.Files
              .AddIfNotContains("/libs/jquery-maskmoney/dist/jquery.maskMoney.min.js");
        }
    }
}
