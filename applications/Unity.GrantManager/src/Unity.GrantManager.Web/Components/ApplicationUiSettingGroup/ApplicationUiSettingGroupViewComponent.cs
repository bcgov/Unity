using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Zones;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Components.ApplicationUiSettingGroup;

public class ApplicationUiSettingGroupViewComponent(IZoneManagementAppService settingsAppService) : AbpViewComponent
{
    public virtual async Task<IViewComponentResult> InvokeAsync()
    {
        throw new NotImplementedException();
    }
}
