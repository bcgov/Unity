using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationBreadcrumbWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/ApplicationBreadcrumb")]
    public class ApplicationBreadcrumbWidgetController : AbpController
    {
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpGet]
        [Route("RefreshApplicationBreadcrumb")]
        public IActionResult Status(Guid applicationId)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid model state for ApplicationBreadcrumbWidgetController: RefreshApplicationBreadcrumb");
                return ViewComponent("ApplicationBreadcrumbWidget");
            }
            return ViewComponent("ApplicationBreadcrumbWidget", new { applicationId });
        }
    }
}
