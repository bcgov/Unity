using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.ApplicationForms;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationFormConfigWidget;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("GrantApplications/Widgets/ApplicationFormConfigWidget")]
public class ApplicationFormConfigWidgetController : AbpController
{
    [HttpGet]
    [Route("Refresh")]
    public IActionResult Refresh(string? configType, ApplicationFormDto? applicationForm)
    {
        if (!ModelState.IsValid)
        {
            Logger.LogWarning("Invalid model state for ApplicationFormConfigWidgetController: Refresh");
            return ViewComponent("ApplicationFormConfigWidget");
        }
        return ViewComponent(typeof(ApplicationFormConfigWidget), new { configType, applicationForm });
    }
}

