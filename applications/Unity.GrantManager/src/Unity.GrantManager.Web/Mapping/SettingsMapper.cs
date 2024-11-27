using AutoMapper;
using Unity.GrantManager.SettingManagement;
using Unity.GrantManager.Web.Components.ApplicationUiSettingGroup;

namespace Unity.GrantManager.Web.Mapping;

public class SettingsMapper : Profile
{
    public SettingsMapper()
    {
        CreateMap<ApplicationUiSettingsDto, ApplicationUiSettingsViewModel>();
    }
}
