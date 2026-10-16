using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.Modules.Shared;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantContacts
{
    [Route("Widget/ApplicantContacts")]
    [Authorize(UnitySelector.ApplicantManagement.Contacts.Default)]
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
