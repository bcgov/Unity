using System;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ProjectInfo
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widget/ApplicantInfo")]
    public class ApplicantInfoController: AbpController
	{
        [HttpGet]
        [Route("Refresh")]
        public IActionResult ApplicantInfo(Guid applicationId, Guid applicationFormVersionId)
        {
            return ViewComponent("ApplicantInfo", new { applicationId, applicationFormVersionId });
        }
    }
}

