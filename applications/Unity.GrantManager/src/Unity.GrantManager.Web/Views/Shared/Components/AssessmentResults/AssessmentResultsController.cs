using System;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.AssessmentResults
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widget/AssessmentResults")]
    public class AssessmentResultsController: AbpController
	{
        [HttpGet]
        [Route("Refresh")]
        public IActionResult AssessmentResults(Guid applicationId, Guid applicationFormVersionId)
        {
            return ViewComponent("AssessmentResults", new { applicationId, applicationFormVersionId });
        }
    }
}

