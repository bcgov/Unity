using AspNetCore;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.GrantManager.SettingManagement;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;

namespace Unity.GrantManager.Web.Components.ApplicationTabsSettingGroup;

public class ApplicationTabsSettingGroupViewComponent : AbpViewComponent
{
    private readonly IApplicationUiSettingsAppService _settingsAppService;
    public ApplicationTabsSettingGroupViewComponent(IApplicationUiSettingsAppService settingsAppService)
    {
        _settingsAppService = settingsAppService;
    }

    public virtual async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await _settingsAppService.GetAsync();
        return View("~/Components/ApplicationTabsSettingGroup/Default.cshtml", model);
    }
}

