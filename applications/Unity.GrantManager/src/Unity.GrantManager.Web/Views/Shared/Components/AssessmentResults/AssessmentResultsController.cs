using System;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Web.Views.Shared.Components.AssessmentResults
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widget/AssessmentResults")]
    public class AssessmentResultsController: AbpController
	{
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);
         
        [HttpGet]
        [Route("Refresh")]
        public IActionResult AssessmentResults(Guid applicationId, Guid applicationFormVersionId)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid model state for AssessmentResultsController: Refresh");
                return ViewComponent("AssessmentResults");
            }
            return ViewComponent("AssessmentResults", new { applicationId, applicationFormVersionId });
        }
    }
}

