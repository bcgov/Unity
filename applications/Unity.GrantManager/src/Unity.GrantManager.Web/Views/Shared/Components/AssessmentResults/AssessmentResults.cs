﻿using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Unity.GrantManager.GrantApplications;
using System.Linq;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Unity.GrantManager.Permissions;

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

        public AssessmentResults(GrantApplicationAppService grantApplicationAppService,
            IAuthorizationService authorizationService)
        {
            _grantApplicationAppService = grantApplicationAppService;
            _authorizationService = authorizationService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId, Guid applicationFormVersionId)
        {
            GrantApplicationDto application = await _grantApplicationAppService.GetAsync(applicationId);
            bool finalDecisionState = GrantApplicationStateGroups.FinalDecisionStates.Contains(application.StatusCode);
            bool isFormEditGranted = await _authorizationService.IsGrantedAsync(GrantApplicationPermissions.AssessmentResults.Edit);
            bool isEditGranted = isFormEditGranted && !finalDecisionState;
            bool isPostEditFieldsAllowed = isEditGranted || (await _authorizationService.IsGrantedAsync(GrantApplicationPermissions.AssessmentResults.EditFinalStateFields) && finalDecisionState);

            AssessmentResultsPageModel model = new()
            {
                ApplicationId = applicationId,
                ApplicationFormId = application.ApplicationForm.Id,
                ApplicationFormVersionId = applicationFormVersionId,
                IsFormEditGranted = isFormEditGranted,
                IsEditGranted = isEditGranted,
                IsPostEditFieldsAllowed = isPostEditFieldsAllowed,

                AssessmentResults = new()
                {
                    ProjectSummary = application.ProjectSummary,
                    RequestedAmount = application.RequestedAmount,
                    TotalProjectBudget = application.TotalProjectBudget,
                    RecommendedAmount = application.RecommendedAmount,
                    ApprovedAmount = application.ApprovedAmount,
                    LikelihoodOfFunding = application.LikelihoodOfFunding,
                    DueDiligenceStatus = application.DueDiligenceStatus,
                    SubStatus = application.SubStatus,
                    DeclineRational = application.DeclineRational,
                    TotalScore = application.TotalScore,
                    Notes = application.Notes,
                    AssessmentResultStatus = application.AssessmentResultStatus,
                    FinalDecisionDate = application.FinalDecisionDate,
                    DueDate = application.DueDate,
                    NotificationDate = application.NotificationDate,
                    RiskRanking = application.RiskRanking,
                }
            };

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
