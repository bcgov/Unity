using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationLinksWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Widgets/ApplicationLinks")]
    [Route("GrantApplications/Widgets/ApplicationLinks")]
    public class ApplicationLinksWidgetController : AbpController
    {
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpGet]
        [Route("RefreshApplicationLinks")]
        public IActionResult ApplicationLinks(Guid applicationId)
        { 
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid model state for ApplicationLinksWidgetController: RefreshApplicationLinks");
                return ViewComponent("ApplicationLinksWidget");
            }
            return ViewComponent("ApplicationLinksWidget", new { applicationId });
        }
    }
}
