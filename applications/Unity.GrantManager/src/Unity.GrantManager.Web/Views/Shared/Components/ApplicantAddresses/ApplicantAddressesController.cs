using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.Modules.Shared;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantAddresses
{
    [ApiController]
    [Route("Widget/ApplicantAddresses")]
    [Authorize(UnitySelector.ApplicantManagement.Addresses.Default)]
    public class ApplicantAddressesController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public async Task<IActionResult> Refresh(Guid applicantId)
        {
            await Task.CompletedTask;

            return ViewComponent("ApplicantAddresses", new { applicantId });
        }
    }
}
