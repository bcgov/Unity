using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.CustomTabWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/CustomTab")]
    public class CustomTabWidgetController : AbpController
    {
        [HttpGet]
        [Route("RefreshCustomTab")]
        public IActionResult CustomTab(Guid correlationId, string correlationProvider, string uiAnchor)
        { 
            return ViewComponent("CustomTabWidget", new { correlationId, correlationProvider, uiAnchor });
        }
    }
}
