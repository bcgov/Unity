using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantOrganizationInfo
{
    [ApiController]
    [Route("Widget/ApplicantOrganizationInfo")]
    public class ApplicantOrganizationInfoController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(Guid applicantId)
        {
            return ViewComponent("ApplicantOrganizationInfo", new { applicantId });
        }
    }
}
