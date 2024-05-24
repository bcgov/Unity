using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("GrantApplications/Widgets/WorksheetInstance")]
public class WorksheetInstanceWidgetController : AbpController
{
    [HttpGet]
    [Route("Refresh")]
    public IActionResult Refresh(Guid correlationId, string correlationProvider, string uiAnchor)
    {
        return ViewComponent(typeof(WorksheetInstanceWidget), new { correlationId, correlationProvider, uiAnchor });
    }
}

