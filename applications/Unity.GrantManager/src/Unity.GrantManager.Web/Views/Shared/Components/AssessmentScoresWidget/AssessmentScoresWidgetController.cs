using System;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Web.Views.Shared.Components.AssessmentScoresWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/AssessmentScores")]
    public class AssessmentScoresWidgetController : AbpController
    {
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpGet]
        [Route("RefreshAssessmentScores")]
        public IActionResult AssessmentScores(Guid assessmentId, Guid currentUserId)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid model state for AssessmentScoresWidgetController: RefreshAssessmentScores");
                return ViewComponent("AssessmentScoresWidget");
            }
            return ViewComponent("AssessmentScoresWidget", new { assessmentId, currentUserId });
        }
        
    }
}
