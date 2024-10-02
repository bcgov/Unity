using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("Flex/Widgets/WorksheetInstance")]
public class WorksheetInstanceWidgetController : AbpController
{
    protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

    [HttpGet]
    [Route("Refresh")]
    public IActionResult Refresh(Guid instanceCorrelationId, string instanceCorrelationProvider, Guid sheetCorrelationId, string sheetCorrelationProvider, string uiAnchor, Guid worksheetId)
    {
        // Check if the model state is valid
        if (!ModelState.IsValid)
        {       
            logger.LogWarning("Invalid model state for WorksheetInstanceWidgetController:Refresh");
            return ViewComponent(typeof(WorksheetInstanceWidget));
        }

        // If the model state is valid, render the view component
        return ViewComponent(typeof(WorksheetInstanceWidget), new { instanceCorrelationId, instanceCorrelationProvider, sheetCorrelationId, sheetCorrelationProvider, uiAnchor, worksheetId });
    }
}
