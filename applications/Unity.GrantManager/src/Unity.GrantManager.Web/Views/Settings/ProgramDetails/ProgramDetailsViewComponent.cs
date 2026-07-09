using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Settings.ProgramDetails;

[Widget(AutoInitialize = true)]
public class ProgramDetailsViewComponent(IApplicationStatusService applicationStatusService) : AbpViewComponent
{
    public virtual async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await applicationStatusService.GetProgramDetailsAsync();
        return View("~/Views/Settings/ProgramDetails/ProgramDetailsViewComponent.cshtml", model);
    }
}
