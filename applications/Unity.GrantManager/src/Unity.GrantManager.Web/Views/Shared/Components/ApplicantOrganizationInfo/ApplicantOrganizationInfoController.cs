using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using Unity.Modules.Shared;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantOrganizationInfo
{
    [ApiController]
    [Route("Widget/ApplicantOrganizationInfo")]
    [Authorize(UnitySelector.ApplicantManagement.ApplicantInfo.Default)]
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
