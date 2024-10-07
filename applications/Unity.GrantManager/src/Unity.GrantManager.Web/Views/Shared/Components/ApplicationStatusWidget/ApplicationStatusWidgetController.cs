using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationStatusWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/Status")]
    public class ApplicationStatusWidgetController : AbpController
    {
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpGet]
        [Route("RefreshStatus")]
        public IActionResult Status(Guid applicationId)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid model state for ApplicationStatusWidgetController: RefreshStatus");
                return ViewComponent("ApplicationStatusWidget");
            }
            return ViewComponent("ApplicationStatusWidget", new { applicationId });
        }
    }
}
