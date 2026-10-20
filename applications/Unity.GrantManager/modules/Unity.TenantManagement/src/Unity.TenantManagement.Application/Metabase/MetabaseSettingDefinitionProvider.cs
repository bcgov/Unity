using Volo.Abp.Settings;
using Volo.Abp.SettingManagement;

namespace Unity.TenantManagement.Metabase;

public class MetabaseSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        foreach (var name in new[] { MetabaseSettings.UserEmails, MetabaseSettings.PermissionSyncedEmails, MetabaseSettings.LastMemberSync })
        {
            context.Add(
                new SettingDefinition(
                    name,
                    defaultValue: null,
                    isVisibleToClients: false,
                    isInherited: false,
                    isEncrypted: false)
                .WithProviders(TenantSettingValueProvider.ProviderName)
            );
        }
    }
}
