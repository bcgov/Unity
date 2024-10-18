using System;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Web.Views.Shared.Components.SummaryWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Widgets/Summary")]
    [Route("GrantApplications/Widgets/Summary")]
    public class SummaryWidgetController : AbpController
    {
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpGet]
        [Route("RefreshSummary")]
        public IActionResult Summary(Guid applicationId, Boolean isReadOnly = false)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid model state for SummaryWidgetController: RefreshSummary");
                return ViewComponent("SummaryWidget");
            }
            return ViewComponent("SummaryWidget", new { applicationId, isReadOnly });
        }
        
    }
}
