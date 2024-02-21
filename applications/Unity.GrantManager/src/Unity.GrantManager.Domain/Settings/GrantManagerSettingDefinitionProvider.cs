using Unity.GrantManager.Localization;
using Volo.Abp.Localization;
using Volo.Abp.Settings;

namespace Unity.GrantManager.Settings;

public class GrantManagerSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition(SettingsConstants.SectorFilterName,string.Empty, L("Setting:GrantManager.Locality.SectorFilter.DisplayName"), L("Setting:GrantManager.Locality.SectorFilter.Description"),isVisibleToClients:true,isInherited:false,isEncrypted:false).WithProviders(TenantSettingValueProvider.ProviderName)
        );
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<GrantManagerResource>(name);
    }
}
