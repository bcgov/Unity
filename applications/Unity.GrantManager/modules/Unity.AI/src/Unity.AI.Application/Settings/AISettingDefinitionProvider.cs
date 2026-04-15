using Unity.AI.Localization;
using Volo.Abp.Localization;
using Volo.Abp.Settings;

namespace Unity.AI.Settings;

public class AISettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition(
                AISettings.AutomaticGenerationEnabled,
                "false",
                L("Setting:AI.AutomaticGenerationEnabled"),
                isVisibleToClients: false,
                isInherited: false,
                isEncrypted: false)
            .WithProviders(TenantSettingValueProvider.ProviderName)
        );

        context.Add(
            new SettingDefinition(
                AISettings.ManualGenerationEnabled,
                "false",
                L("Setting:AI.ManualGenerationEnabled"),
                isVisibleToClients: false,
                isInherited: false,
                isEncrypted: false)
            .WithProviders(TenantSettingValueProvider.ProviderName)
        );
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AIResource>(name);
    }
}
