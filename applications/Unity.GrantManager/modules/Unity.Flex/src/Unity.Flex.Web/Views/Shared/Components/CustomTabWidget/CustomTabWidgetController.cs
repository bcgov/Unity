using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.CustomTabWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/CustomTab")]
    public class CustomTabWidgetController : AbpController
    {
        [HttpGet]
        [Route("RefreshCustomTab")]
        public IActionResult CustomTab(Guid instanceCorrelationId, string instanceCorrelationProvider, Guid sheetCorrelationId, string sheetCorrelationProvider, string uiAnchor, string name, string title)
        { 
            return ViewComponent("CustomTabWidget", new { instanceCorrelationId, instanceCorrelationProvider, sheetCorrelationId, sheetCorrelationProvider, uiAnchor, name, title });
        }
    }
}