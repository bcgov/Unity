using System;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Web.Views.Shared.Components.ProjectInfo
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widget/ApplicantInfo")]
    public class ApplicantInfoController: AbpController
	{
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpGet]
        [Route("Refresh")]
        public IActionResult ApplicantInfo(Guid applicationId, Guid applicationFormVersionId)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid model state for ApplicantInfoController: Refresh");
                return ViewComponent("ApplicantInfo");
            }
            return ViewComponent("ApplicantInfo", new { applicationId, applicationFormVersionId });
        }
    }
}

