using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Assessments;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Scoresheets;
using Volo.Abp.ObjectMapping;

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
        private readonly IScoresheetRepository _scoresheetRepository;
        public AssessmentScoresWidgetViewComponent(IAssessmentRepository assessmentRepository, IScoresheetRepository scoresheetRepository)
        {
            _assessmentRepository = assessmentRepository;
            _scoresheetRepository = scoresheetRepository;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid assessmentId, Guid currentUserId)
        {
            if(assessmentId == Guid.Empty)
            {
                return View(new AssessmentScoresWidgetViewModel());
            }
            var assessment = await _assessmentRepository.GetAsync(assessmentId);
            ScoresheetDto? scoresheetDto = null;
            if (assessment.ScoresheetId != null)
            {
                var scoresheet = await _scoresheetRepository.GetWithChildrenAsync(assessment.ScoresheetId ?? Guid.Empty);
                if(scoresheet != null)
                {
                    scoresheetDto = ObjectMapper.Map<Scoresheet, ScoresheetDto?>(scoresheet);
                }
            }
            AssessmentScoresWidgetViewModel model = new()
            {
                AssessmentId = assessmentId,
                FinancialAnalysis = assessment.FinancialAnalysis,
                EconomicImpact = assessment.EconomicImpact,
                InclusiveGrowth = assessment.InclusiveGrowth,
                CleanGrowth = assessment.CleanGrowth,
                Status = assessment.Status,
                CurrentUserId = currentUserId,
                AssessorId = assessment.AssessorId,
                Scoresheet = scoresheetDto,
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
