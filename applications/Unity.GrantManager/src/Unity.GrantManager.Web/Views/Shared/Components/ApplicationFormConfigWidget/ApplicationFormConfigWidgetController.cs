using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.ApplicationForms;
using System.Threading.Tasks;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationFormConfigWidget;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("GrantApplications/Widgets/ApplicationFormConfigWidget")]
public class ApplicationFormConfigWidgetController(IApplicationFormAppService applicationFormAppService) : AbpController
{
    [HttpGet]
    [Route("Refresh")]
    public async Task<IActionResult> Refresh(string? configType, ApplicationFormDto? applicationForm)
    {
        if (!ModelState.IsValid)
        {
            Logger.LogWarning("Invalid model state for ApplicationFormConfigWidgetController: Refresh");
            return ViewComponent("ApplicationFormConfigWidget");
        }
        if (applicationForm == null)
        {
            return BadRequest();
        }

        applicationForm = await applicationFormAppService.GetAsync(applicationForm.Id);
        if (applicationForm.IsHiddenFromList)
        {
            return NotFound();
        }
        return ViewComponent(typeof(ApplicationFormConfigWidget), new { configType, applicationForm });
    }
}

