using System.Collections.Generic;
using Unity.GrantManager.Localization;
using Unity.Payments.Settings;
using Volo.Abp.Localization;
using Volo.Abp.Settings;

namespace Unity.GrantManager.Settings;

public class GrantManagerSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition(
                SettingsConstants.SectorFilterName,
                string.Empty,
                L("Setting:GrantManager.Locality.SectorFilter.DisplayName"),
                L("Setting:GrantManager.Locality.SectorFilter.Description"),
                isVisibleToClients: true,
                isInherited: false,
                isEncrypted: false).WithProviders(TenantSettingValueProvider.ProviderName)
        );

        var tabSettings = new Dictionary<string, bool>
        {
            { SettingsConstants.UI.Tabs.Submission, true },
            { SettingsConstants.UI.Tabs.Assessment, true },
            { SettingsConstants.UI.Tabs.Project, true },
            { SettingsConstants.UI.Tabs.Applicant, true },
            { SettingsConstants.UI.Tabs.Payments, true },
            { SettingsConstants.UI.Tabs.FundingAgreement, true }
        };

        foreach (var setting in tabSettings)
        {
            AddSettingDefinition(context, setting.Key, setting.Value.ToString());
        }

        context.Add(
            new SettingDefinition(
                SettingsConstants.UI.Zones,
                string.Empty,
                L($"Setting:{SettingsConstants.UI.Zones}.DisplayName"),
                L($"Setting:{SettingsConstants.UI.Zones}.Description"),
                isVisibleToClients: false,
                isInherited: false,
                isEncrypted: false)
            .WithProviders(
                TenantSettingValueProvider.ProviderName, 
                FormSettingValueProvider.ProviderName)
        );

        AddBackgroundJobSettingDefinition(context);
    }

    private static void AddBackgroundJobSettingDefinition(ISettingDefinitionContext currentContext)
    {
        var backGroundSchedules = new Dictionary<string, string>
        {
            { SettingsConstants.BackgroundJobs.IntakeResync_NumDaysToCheck, "-4" },
             // 23 = 11 pm   So 23 + 8 UTC = 7 also at 19 = 11 am
            { SettingsConstants.BackgroundJobs.IntakeResync_Expression, "0 0 7,19 1/1 * ? *" },
            // 24 = 12 am   So 24 + 8 UTC = 8
            { PaymentSettingsConstants.BackgroundJobs.CasPaymentsReconciliation_ProducerExpression, "0 0 8 1/1 * ? *" },
            // 24 = 1 am   So 24 + 8 UTC = 9
            { PaymentSettingsConstants.BackgroundJobs.CasFinancialNotificationSummary_ProducerExpression, "0 0 9 1/1 * ? *" },
            // Run hourly
            { SettingsConstants.BackgroundJobs.DataHealthCheckMonitor_Expression, "0 0 * 1/1 * ? *" }
        };

        foreach (var setting in backGroundSchedules)
        {
            AddSettingDefinition(currentContext, setting.Key, setting.Value.ToString());
        }

    }

    private static void AddSettingDefinition(ISettingDefinitionContext currentContext, string settingName, string defaultValue = "True")
    {
        var displayName = L($"Setting:{settingName}.DisplayName");
        var description = L($"Setting:{settingName}.Description");

        currentContext.Add(
            new SettingDefinition(
                settingName,
                defaultValue,
                displayName,
                description,
                isVisibleToClients: true,
                isInherited: false,
                isEncrypted: false).WithProviders(TenantSettingValueProvider.ProviderName)
        );
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<GrantManagerResource>(name);
    }
}
