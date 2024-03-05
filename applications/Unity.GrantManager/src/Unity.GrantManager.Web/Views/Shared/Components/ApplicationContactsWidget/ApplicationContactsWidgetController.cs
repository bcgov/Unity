using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationContactsWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Widgets/ApplicationContacts")]
    [Route("GrantApplications/Widgets/ApplicationContacts")]
    public class ApplicationContactsWidgetController : AbpController
    {
        [HttpGet]
        [Route("RefreshApplicationContacts")]
        public IActionResult ApplicationContacts(Guid applicationId, Boolean isReadOnly = false)
        { 
            return ViewComponent("ApplicationContactsWidget", new { applicationId, isReadOnly });
        }
    }
}
