using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationActionWidget;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("GrantApplications/Widgets/ApplicationActionWidget")]
public class ApplicationActionWidgetController : AbpController
{
    protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

    [HttpGet]
    [Route("Refresh")]
    public IActionResult Refresh(Guid applicationId)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Invalid model state for ApplicationActionWidgetController: Refresh");
            return ViewComponent("ApplicationActionWidget");
        }
        return ViewComponent(typeof(ApplicationActionWidget), new { applicationId });
    }
}

