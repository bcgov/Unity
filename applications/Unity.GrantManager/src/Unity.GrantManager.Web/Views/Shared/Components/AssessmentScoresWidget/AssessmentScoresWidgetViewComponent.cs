using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Assessments;

namespace Unity.GrantManager.Web.Views.Shared.Components.AssessmentScoresWidget
{
    [Widget(
        RefreshUrl = "Widgets/AssessmentScores/RefreshAssessmentScores",
        ScriptTypes = new[] { typeof(AssessmentScoresWidgetScriptBundleContributor) },
        StyleTypes = new[] { typeof(AssessmentScoresWidgetStyleBundleContributor) },
        AutoInitialize = true)]
    public class AssessmentScoresWidgetViewComponent : AbpViewComponent
    {
        private readonly IAssessmentRepository _assessmentRepository;
        public AssessmentScoresWidgetViewComponent(IAssessmentRepository assessmentRepository)
        {
            _assessmentRepository = assessmentRepository;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid assessmentId)
        {
            if(assessmentId == Guid.Empty)
            {
                return View(new AssessmentScoresWidgetViewModel());
            }
            var assessment = await _assessmentRepository.GetAsync(assessmentId);

            AssessmentScoresWidgetViewModel model = new()
            {
                AssessmentId = assessmentId,
                FinancialAnalysis = assessment.FinancialAnalysis,
                EconomicImpact = assessment.EconomicImpact,
                InclusiveGrowth = assessment.InclusiveGrowth,
                CleanGrowth = assessment.CleanGrowth,
                Status = assessment.Status,
            };

            return View(model);
        } 
        
    }

    public class AssessmentScoresWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/AssessmentScoresWidget/Default.css");
        }
    }

    public class AssessmentScoresWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/AssessmentScoresWidget/Default.js");
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}
