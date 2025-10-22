using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantAddresses
{
    [ApiController]
    [Route("Widget/ApplicantAddresses")]
    public class ApplicantAddressesController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public async Task<IActionResult> Refresh(Guid applicantId)
        {
            return ViewComponent("ApplicantAddresses", new { applicantId });
        }
    }
}
