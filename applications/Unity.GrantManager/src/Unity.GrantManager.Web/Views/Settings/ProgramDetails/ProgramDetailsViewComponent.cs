using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.GrantManager.SettingManagement;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Settings.ProgramDetails;

[Widget(AutoInitialize = true)]
public class ProgramDetailsViewComponent(IProgramDetailsAppService programDetailsAppService) : AbpViewComponent
{
    public virtual async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await programDetailsAppService.GetProgramDetailsAsync();
        return View("~/Views/Settings/ProgramDetails/ProgramDetailsViewComponent.cshtml", model);
    }
}
