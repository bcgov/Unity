using Unity.Reporting.Localization;
using Unity.Reporting.Settings;
using Volo.Abp.Localization;
using Volo.Abp.Settings;

namespace Unity.Reporting.Domain.Settings;

/// <summary>
/// ABP Framework setting definition provider for the Unity.Reporting module configuration settings.
/// Registers all reporting-related settings with the ABP settings system including their default values,
/// localization keys, visibility, inheritance rules, and security constraints. Ensures settings are properly
/// configured with appropriate access levels and provider restrictions for secure configuration management.
/// </summary>
public class ReportingSettingDefinitionProvider : SettingDefinitionProvider
{
    /// <summary>
    /// Defines all Unity.Reporting module settings and registers them with the ABP settings system.
    /// Configures the TenantViewRole setting as the primary tenant-level setting for database role assignment.
    /// The global ViewRole setting is kept for backward compatibility but is deprecated.
    /// </summary>
    /// <param name="context">The setting definition context for registering setting definitions with the ABP settings framework.</param>
    public override void Define(ISettingDefinitionContext context)
    {
        // [DEPRECATED] Global ViewRole setting - kept for backward compatibility only
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

        // Primary tenant-level setting for database role assignment
        context.Add(
            new SettingDefinition(
                ReportingSettings.TenantViewRole,
                defaultValue: string.Empty,
                L("Setting:GrantManager.Reporting.TenantViewRole.DisplayName"),
                L("Setting:GrantManager.Reporting.TenantViewRole.Description"),
                isVisibleToClients: false,
                isInherited: false,
                isEncrypted: false
            ).WithProviders("T") // Tenant-level only
        );
    }

    /// <summary>
    /// Creates a localizable string using the Unity.Reporting localization resource.
    /// Provides a convenient method for creating localized setting display names and descriptions
    /// that will be resolved at runtime based on the current user's language settings.
    /// </summary>
    /// <param name="name">The localization key for the setting display text.</param>
    /// <returns>A localizable string that will be resolved using the ReportingResource localization provider.</returns>
    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<ReportingResource>(name);
    }
}