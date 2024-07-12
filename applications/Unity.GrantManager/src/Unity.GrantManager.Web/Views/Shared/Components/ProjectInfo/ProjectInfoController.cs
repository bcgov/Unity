using System;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ProjectInfo
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widget/ProjectInfo")]
    public class ProjectInfoController: AbpController
	{
        [HttpGet]
        [Route("Refresh")]
        public IActionResult ProjectInfo(Guid applicationId, Guid applicationFormVersionId)
        {
            return ViewComponent("ProjectInfo", new { applicationId, applicationFormVersionId });
        }
    }
}

