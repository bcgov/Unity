using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Unity.GrantManager.GrantApplications;
using System.Linq;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;

namespace Unity.GrantManager.Web.Views.Shared.Components.AssessmentResults
{

    [Widget(
        RefreshUrl = "Widget/AssessmentResults/Refresh",
        ScriptTypes = new[] { typeof(AssessmentResultsScriptBundleContributor) },
        StyleTypes = new[] { typeof(AssessmentResultsStyleBundleContributor) },
        AutoInitialize = true)]
    public class AssessmentResults : AbpViewComponent
    {
        private readonly GrantApplicationAppService _grantApplicationAppService;

        public AssessmentResults(GrantApplicationAppService grantApplicationAppService)
        {
            _grantApplicationAppService = grantApplicationAppService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
        {
            GrantApplicationDto application = await _grantApplicationAppService.GetAsync(applicationId);

            AssessmentResultsPageModel model = new()
            {
                ApplicationId = applicationId
            };

            // Need to leverage state machine / domain layer for this logic
            GrantApplicationState[] finalDecisionArr =  {
                GrantApplicationState.GRANT_APPROVED,
                GrantApplicationState.GRANT_NOT_APPROVED,
                GrantApplicationState.CLOSED,
                GrantApplicationState.WITHDRAWN,
            };
            model.IsFinalDecisionMade = finalDecisionArr.Contains(application.StatusCode);

            model.AssessmentResults = new()
            {
                ProjectSummary = application.ProjectSummary,
                RequestedAmount = application.RequestedAmount,
                TotalProjectBudget = application.TotalProjectBudget,
                RecommendedAmount = application.RecommendedAmount,
                ApprovedAmount = application.ApprovedAmount,
                LikelihoodOfFunding = application.LikelihoodOfFunding,
                DueDilligenceStatus = application.DueDilligenceStatus,
                Recommendation = application.Recommendation,
                DeclineRational = application.DeclineRational,
                TotalScore = application.TotalScore,
                Notes = application.Notes,
                AssessmentResultStatus = application.AssessmentResultStatus
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
