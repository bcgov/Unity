using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationContactsWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/ApplicationContacts")]
    public class ApplicationContactsWidgetController : AbpController
    {
        [HttpGet]
        [Route("RefreshApplicationContacts")]
        public IActionResult ApplicationContacts(Guid applicationId)
        { 
            return ViewComponent("ApplicationContactsWidget", new { applicationId });
        }
    }
}
