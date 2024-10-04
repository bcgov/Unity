using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.CustomTabWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/CustomTab")]
    public class CustomTabWidgetController : AbpController
    {
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpGet]
        [Route("RefreshCustomTab")]
        public IActionResult CustomTab(Guid instanceCorrelationId, string instanceCorrelationProvider, Guid sheetCorrelationId, string sheetCorrelationProvider, string uiAnchor, string name, string title)
        { 
            // Check if the model state is valid
            if (!ModelState.IsValid)
            {       
                logger.LogWarning("Invalid model state for CustomTabWidgetController");
                return ViewComponent("CustomTabWidget");
            }

            // If the model state is valid, render the view component
            return ViewComponent("CustomTabWidget", new { instanceCorrelationId, instanceCorrelationProvider, sheetCorrelationId, sheetCorrelationProvider, uiAnchor, name, title });
        }
    }
}