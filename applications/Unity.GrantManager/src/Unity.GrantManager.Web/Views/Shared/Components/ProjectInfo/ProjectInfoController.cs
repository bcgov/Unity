using System;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Web.Views.Shared.Components.ProjectInfo
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widget/ProjectInfo")]
    public class ProjectInfoController: AbpController
	{
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpGet]
        [Route("Refresh")]
        public IActionResult ProjectInfo(Guid applicationId)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid model state for ProjectInfoController: Refresh");
                return ViewComponent("ProjectInfo");
            }
            return ViewComponent("ProjectInfo", new { applicationId, Guid.Empty });
        }
    }
}

