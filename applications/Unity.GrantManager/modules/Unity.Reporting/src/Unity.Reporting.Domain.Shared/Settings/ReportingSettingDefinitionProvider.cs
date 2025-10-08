using Unity.Reporting.Localization;
using Unity.Reporting.Settings;
using Volo.Abp.Localization;
using Volo.Abp.Settings;

namespace Unity.Reporting.Domain.Settings;

public class ReportingSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition(
                ReportingSettings.ViewRole,
                defaultValue: string.Empty,
                L("Setting:GrantManager.Reporting.ViewRole.DisplayName"),
                L("Setting:GrantManager.Reporting.ViewRole.Description"),
                isVisibleToClients: false,
                isInherited: false,
                isEncrypted: false
            ).WithProviders(GlobalSettingValueProvider.ProviderName) // Host-level only
        );
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<ReportingResource>(name);
    }
}