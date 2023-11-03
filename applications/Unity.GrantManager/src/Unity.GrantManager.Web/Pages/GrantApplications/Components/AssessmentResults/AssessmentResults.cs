using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Unity.GrantManager.GrantApplications;
using System.Linq;

namespace Unity.GrantManager.Web.Pages.GrantApplications.Components.AssessmentResults
{

    [Widget(
    ScriptFiles = new[] {
        "/Pages/GrantApplications/Components/AssessmentResults/Default.js",
        "/libs/jquery-maskmoney/dist/jquery.maskMoney.min.js",
    },
    StyleFiles = new[] {
        "/Pages/GrantApplications/Components/AssessmentResults/Default.css"
    })]
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

            AssessmentResultsPageModel model = new();

            model.ApplicationId = applicationId;

            GrantApplicationState[] finalDecisionArr =  {
                GrantApplicationState.GRANT_APPROVED,
                GrantApplicationState.GRANT_NOT_APPROVED,
                GrantApplicationState.CLOSED,
                GrantApplicationState.WITHDRAWN,
            };
            model.isFinalDecisionMade = finalDecisionArr.Contains(application.StatusCode);

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
}
