using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.GrantManager.SettingManagement;
using Unity.GrantManager.Zones;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Components.ApplicationUiSettingGroup;

public class ApplicationUiSettingGroupViewComponent(IZoneManagementAppService settingsAppService) : AbpViewComponent
{
    public virtual async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await settingsAppService.GetAsync();
        var viewModel = ObjectMapper.Map<ApplicationUiSettingsDto, ApplicationUiSettingsViewModel>(model!);
        return View("~/Components/ApplicationUiSettingGroup/Default.cshtml", viewModel);
    }
}
