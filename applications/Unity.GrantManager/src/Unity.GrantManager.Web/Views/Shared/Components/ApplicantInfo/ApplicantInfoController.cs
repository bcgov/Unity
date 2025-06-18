using System;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantInfo
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widget/ApplicantInfo")]
    public class ApplicantInfoController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult ApplicantInfo(Guid applicationId, Guid applicationFormVersionId)
        {
            if (!ModelState.IsValid)
            {
                Logger.LogWarning("Invalid model state for ApplicantInfoController: Refresh");

                return ViewComponent("ApplicantInfo");
            }
            return ViewComponent("ApplicantInfo", new { applicationId, applicationFormVersionId });
        }
    }
}

