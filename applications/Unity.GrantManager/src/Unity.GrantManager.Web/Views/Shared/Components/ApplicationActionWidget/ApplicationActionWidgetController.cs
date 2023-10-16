using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationActionWidget;


[ApiExplorerSettings(IgnoreApi = true)]
[Route("GrantApplications/Widgets/ApplicationActionWidget")]
public class ApplicationActionWidgetController : AbpController
{
    [HttpGet]
    [Route("Refresh")]
    public IActionResult Refresh(Guid applicationId)
    {
        return ViewComponent(typeof(ApplicationActionWidget), new { applicationId });
    }
}

