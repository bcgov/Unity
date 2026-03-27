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
                AISettings.ScoringAssistantEnabled,
                "false",
                L("Setting:AI.ScoringAssistantEnabled"),
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
