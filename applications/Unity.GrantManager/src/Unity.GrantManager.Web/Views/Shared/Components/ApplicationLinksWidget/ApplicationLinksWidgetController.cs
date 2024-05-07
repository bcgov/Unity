using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationLinksWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Widgets/ApplicationLinks")]
    [Route("GrantApplications/Widgets/ApplicationLinks")]
    public class ApplicationLinksWidgetController : AbpController
    {
        [HttpGet]
        [Route("RefreshApplicationLinks")]
        public IActionResult ApplicationLinks(Guid applicationId)
        { 
            return ViewComponent("ApplicationLinksWidget", new { applicationId });
        }
    }
}
