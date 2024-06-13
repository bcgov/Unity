using System;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.AssessmentScoresheet
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/AssessmentScoresheet")]
    public class AssessmentScoresheetController : AbpController
    {
        
        [HttpGet]
        [Route("RefreshAssessmentScoresheet")]
        public IActionResult AssessmentScores(Guid assessmentId, Guid currentUserId)
        {
            return ViewComponent("AssessmentScoresheet", new { assessmentId, currentUserId });
        }
        
    }
}
