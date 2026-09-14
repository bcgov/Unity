using Volo.Abp.Settings;
using Volo.Abp.SettingManagement;

namespace Unity.TenantManagement.Metabase;

public class MetabaseSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition(
                MetabaseSettings.UserEmails,
                defaultValue: null,
                isVisibleToClients: false,
                isInherited: false,
                isEncrypted: false)
            .WithProviders(GlobalSettingValueProvider.ProviderName, TenantSettingValueProvider.ProviderName)
        );
    }
}
