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
using Unity.Flex.Domain.ScoresheetInstances;

namespace Unity.GrantManager.Web.Views.Shared.Components.AssessmentScoresheet
{
    [Widget(
        RefreshUrl = "Widgets/AssessmentScoresheet/RefreshAssessmentScoresheet",
        ScriptTypes = new[] { typeof(AssessmentScoresheetScriptBundleContributor) },
        StyleTypes = new[] { typeof(AssessmentScoresheetStyleBundleContributor) },
        AutoInitialize = true)]
    public class AssessmentScoresheetViewComponent : AbpViewComponent
    {
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly IScoresheetRepository _scoresheetRepository;
        private readonly IScoresheetInstanceRepository _scoresheetInstanceRepository;
        public AssessmentScoresheetViewComponent(IAssessmentRepository assessmentRepository, IScoresheetRepository scoresheetRepository, IScoresheetInstanceRepository scoresheetInstanceRepository)
        {
            _assessmentRepository = assessmentRepository;
            _scoresheetRepository = scoresheetRepository;
            _scoresheetInstanceRepository = scoresheetInstanceRepository;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid assessmentId, Guid currentUserId)
        {
            if(assessmentId == Guid.Empty)
            {
                return View(new AssessmentScoresheetViewModel());
            }
            var assessment = await _assessmentRepository.GetAsync(assessmentId);
            var scoresheetInstance = await _scoresheetInstanceRepository.GetByCorrelationAsync(assessment.Id);
            ScoresheetDto? scoresheetDto = null;
            if (scoresheetInstance != null)
            {
                var scoresheet = await _scoresheetRepository.GetWithChildrenAsync(scoresheetInstance.ScoresheetId);
                if(scoresheet != null)
                {
                    scoresheetDto = ObjectMapper.Map<Scoresheet, ScoresheetDto?>(scoresheet);
                }
            }
            AssessmentScoresheetViewModel model = new()
            {
                AssessmentId = assessmentId,
                Status = assessment.Status,
                CurrentUserId = currentUserId,
                AssessorId = assessment.AssessorId,
                Scoresheet = scoresheetDto,
            };

            return View(model);
        } 
        
    }

    public class AssessmentScoresheetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/AssessmentScoresheet/Default.css");
        }
    }

    public class AssessmentScoresheetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/AssessmentScoresheet/Default.js");
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}
