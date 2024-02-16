using Volo.Abp.Settings;

namespace Unity.GrantManager.Settings;

public class GrantManagerSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition("SectorFilter","",null,null,isVisibleToClients:true,isInherited:false,isEncrypted:false).WithProviders(TenantSettingValueProvider.ProviderName)
        );
    }
}
