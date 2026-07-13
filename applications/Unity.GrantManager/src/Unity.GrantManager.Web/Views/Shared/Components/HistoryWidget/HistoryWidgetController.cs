using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.HistoryWidget;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("GrantApplications/Widgets/History")]
public class HistoryWidgetController : AbpController
{
    [HttpGet]
    [Route("RefreshHistory")]
    public IActionResult HistoryWidget(Guid applicationId)
    {
        if (!ModelState.IsValid)
        {
            return ViewComponent("HistoryWidget");
        }

        return ViewComponent("HistoryWidget", new { applicationId });
    }
}
