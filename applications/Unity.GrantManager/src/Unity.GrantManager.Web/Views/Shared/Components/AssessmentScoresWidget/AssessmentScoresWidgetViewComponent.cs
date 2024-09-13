using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Assessments;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Scoresheets;
using System.Linq;
using Unity.Flex.Worksheets;
using Unity.Flex.Web.Views.Shared.Components;
using Unity.Flex.Worksheets.Values;

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
        private readonly IScoresheetInstanceRepository _scoresheetInstanceRepository;
        public AssessmentScoresWidgetViewComponent(IAssessmentRepository assessmentRepository, IScoresheetRepository scoresheetRepository, IScoresheetInstanceRepository scoresheetInstanceRepository)
        {
            _assessmentRepository = assessmentRepository;
            _scoresheetRepository = scoresheetRepository;
            _scoresheetInstanceRepository = scoresheetInstanceRepository;
        }
        

        public async Task<IViewComponentResult> InvokeAsync(Guid assessmentId, Guid currentUserId)
        {
            if (assessmentId == Guid.Empty)
            {
                return View(new AssessmentScoresWidgetViewModel());
            }
            var assessment = await _assessmentRepository.GetAsync(assessmentId);
            var scoresheetInstance = await _scoresheetInstanceRepository.GetByCorrelationAsync(assessment.Id);
            ScoresheetDto? scoresheetDto = null;
            if (scoresheetInstance != null)
            {
                var scoresheet = await _scoresheetRepository.GetWithChildrenAsync(scoresheetInstance.ScoresheetId);
                if (scoresheet != null)
                {
                    scoresheetDto = ObjectMapper.Map<Scoresheet, ScoresheetDto?>(scoresheet);
                    foreach (var answer in scoresheetInstance.Answers)
                    {
                        foreach (var section in scoresheetDto!.Sections)
                        {
                            var question = section.Fields.FirstOrDefault(q => q.Id == answer.QuestionId);
                            if (question != null)
                            {
                                switch (question.Type)
                                {
                                    case QuestionType.Number:
                                        {
                                            question.Answer = ValueResolver.Resolve(answer.CurrentValue!, CustomFieldType.Numeric)!.ToString();
                                            break;
                                        }
                                    case QuestionType.YesNo:
                                        {
                                            question.Answer = ValueResolver.Resolve(answer.CurrentValue!, CustomFieldType.YesNo)!.ToString();
                                            break;
                                        }
                                    case QuestionType.Text:
                                        {
                                            question.Answer = ValueResolver.Resolve(answer.CurrentValue!, CustomFieldType.Text)!.ToString();
                                            break;
                                        }
                                    case QuestionType.SelectList:
                                        {
                                            question.Answer = ValueResolver.Resolve(answer.CurrentValue!, CustomFieldType.SelectList)!.ToString();
                                            break;
                                        }
                                }
                                
                            }
                        }
                    }
                }
            }
            AssessmentScoresWidgetViewModel model = new()
            {
                AssessmentId = assessmentId,
                Scoresheet = scoresheetDto,
                FinancialAnalysis = assessment.FinancialAnalysis,
                EconomicImpact = assessment.EconomicImpact,
                InclusiveGrowth = assessment.InclusiveGrowth,
                CleanGrowth = assessment.CleanGrowth,
                Status = assessment.Status,
                CurrentUserId = currentUserId,
                AssessorId = assessment.AssessorId,
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
