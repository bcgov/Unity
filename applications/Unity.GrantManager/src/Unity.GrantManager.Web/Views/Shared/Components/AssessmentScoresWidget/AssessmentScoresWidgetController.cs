using System;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.AssessmentScoresWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/AssessmentScores")]
    public class AssessmentScoresWidgetController : AbpController
    {
        
        [HttpGet]
        [Route("RefreshAssessmentScores")]
        public IActionResult AssessmentScores(Guid assessmentId, Guid currentUserId)
        {
            return ViewComponent("AssessmentScoresWidget", new { assessmentId, currentUserId });
        }
        
    }
}
