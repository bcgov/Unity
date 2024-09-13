using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("Flex/Widgets/WorksheetInstance")]
public class WorksheetInstanceWidgetController : AbpController
{
    [HttpGet]
    [Route("Refresh")]
    public IActionResult Refresh(Guid instanceCorrelationId, string instanceCorrelationProvider, Guid sheetCorrelationId, string sheetCorrelationProvider, string uiAnchor, Guid worksheetId)
    {
        return ViewComponent(typeof(WorksheetInstanceWidget), new { instanceCorrelationId, instanceCorrelationProvider, sheetCorrelationId, sheetCorrelationProvider, uiAnchor, worksheetId });
    }
}
