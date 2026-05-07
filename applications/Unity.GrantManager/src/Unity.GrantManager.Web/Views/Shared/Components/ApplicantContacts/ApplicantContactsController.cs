using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantContacts
{
    [Route("Widget/ApplicantContacts")]
    public class ApplicantContactsController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public async Task<IActionResult> Refresh(Guid applicantId)
        {
            if (!ModelState.IsValid) return BadRequest();

            await Task.CompletedTask;

            return ViewComponent("ApplicantContacts", new { applicantId });
        }
    }
}
